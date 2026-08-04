using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Models.Llm;
using MiyakoCarryService.Server.Services.Llm.Providers;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.Services.Llm
{
    /// <summary>
    /// 服务端 LLM 商人对话分发器。玩家在与宫子商人对话中输入任意自然语言文本时被调用：
    /// <list type="bullet">
    ///   <item>若 LLM 未启用，直接返回 <see cref="LlmDispatchResult.NotHandled()"/>，由 <see cref="MiyakoCarryService.Server.ChatBot.MiyakoChatBot"/> 走原 unknown-command 流程。</item>
    ///   <item>若启用了限流且本玩家超限，返回 Cool-down 提示。</item>
    ///   <item>否则发送 "interpreting" 提示，调 ILlmProvider，按意图路由到 <see cref="QuestController"/> 或纯聊天回复。</item>
    /// </list>
    /// 限流：按 sessionId 维护令牌桶，<see cref="McsPluginServerConfig.LlmMaxMessagesPerMinute"/> 控制每分钟最大消息数。
    /// </summary>
    [Injectable(InjectionType.Singleton)]
    public class LlmDispatcherService(
        ConfigService configService,
        QuestController questController,
        MailSendService mailSendService,
        ServerLocalisationService serverLocalisationService,
        ISptLogger<LlmDispatcherService> logger
    )
    {
        private readonly ConcurrentDictionary<MongoId, RateBucket> _bucketsPerSession = new();

        /// <summary>
        /// 尝试用 LLM 解释并处理用户消息。返回是否被处理（true 时由调用方返回 <c>request.DialogId</c>）。
        /// </summary>
        public async Task<LlmDispatchResult> TryDispatchAsync(MongoId sessionId, string text)
        {
            var serverConfig = configService.GetMcsPluginConfig().ServerConfig;

            if (!serverConfig.LlmEnabled)
            {
                return LlmDispatchResult.NotHandled();
            }

            // 限流：每分钟 LlmMaxMessagesPerMinute 条
            var maxPerMinute = serverConfig.LlmMaxMessagesPerMinute > 0 ? serverConfig.LlmMaxMessagesPerMinute : 10;
            var bucket = _bucketsPerSession.GetOrAdd(sessionId, _ => new RateBucket(maxPerMinute));
            if (!bucket.TryConsume())
            {
                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERLLMCOOLDOWN,
                    null);
                return LlmDispatchResult.Handled();
            }

            // 发送 "interpreting" 提示
            mailSendService.SendLocalisedNpcMessageToPlayer(
                sessionId,
                TraderService.MiyakoTraderId,
                MessageType.NpcTraderMessage,
                Locales.MIYAKOTRADERLLMINTERPRETING,
                null);

            var settings = new LlmProviderSettings
            {
                ApiKey = serverConfig.LlmApiKey,
                BaseUrl = serverConfig.LlmBaseUrl,
                Model = serverConfig.LlmModel,
                SystemPrompt = MiyakoTraderPromptTemplates.BuildSystemPrompt(serverConfig.LlmSystemPrompt, BuildSpawnTypeHelp()),
                Temperature = serverConfig.LlmTemperature,
                MaxTokens = serverConfig.LlmMaxTokens,
                TimeoutSec = serverConfig.LlmTimeoutSec,
            };

            var provider = CreateProvider(serverConfig.LlmProvider);
            if (provider == null)
            {
                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERLLMDISABLED,
                    null);
                return LlmDispatchResult.Handled();
            }

            LlmIntent intent;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(serverConfig.LlmTimeoutSec > 0 ? serverConfig.LlmTimeoutSec : 30));
                intent = await provider.InterpretAsync(text, settings, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Error("LlmDispatcher 异常: " + ex);
                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERLLMERROR,
                    null);
                return LlmDispatchResult.Handled();
            }

            if (intent == null || intent.IsError)
            {
                logger.Warning("LlmDispatcher 意图解析失败: " + (intent?.Error ?? "null"));
                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERLLMERROR,
                    null);
                return LlmDispatchResult.Handled();
            }

            // 处理意图
            if (intent.IsCommand && intent.Order != null)
            {
                var order = intent.Order;
                if (!ValidateOrder(order))
                {
                    mailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        serverLocalisationService.GetText(Locales.MIYAKOTRADERCOMMANDERROR),
                        null);
                    return LlmDispatchResult.Handled();
                }

                var spawnType = configService.TryGetSpawnType(order.SpawnTypeIndex);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERLLMCONFIRMORDER,
                    null);

                questController.CreateOrderQuest(sessionId, order.Players, spawnType, order.Level, order.Duration);
                return LlmDispatchResult.Handled();
            }

            if (intent.IsCommand && intent.Ticket != null)
            {
                var ticket = intent.Ticket;
                if (!ValidateTicket(ticket))
                {
                    mailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        serverLocalisationService.GetText(Locales.MIYAKOTRADERCOMMANDERROR),
                        null);
                    return LlmDispatchResult.Handled();
                }

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERLLMCONFIRMTICKET,
                    null);

                questController.CreateTicketQuest(sessionId, ticket.Percent);
                return LlmDispatchResult.Handled();
            }

            if (intent.IsReply)
            {
                mailSendService.SendDirectNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    intent.ReplyText,
                    null);
                return LlmDispatchResult.Handled();
            }

            // LLM 既未识别指令也未给出回复：友好兜底
            mailSendService.SendLocalisedNpcMessageToPlayer(
                sessionId,
                TraderService.MiyakoTraderId,
                MessageType.NpcTraderMessage,
                Locales.MIYAKOTRADERUNRECOGNIZEDCOMMAND,
                null);
            return LlmDispatchResult.Handled();
        }

        private static bool ValidateOrder(OrderIntent order)
        {
            return order != null
                && order.Players >= MiyakoTraderPromptTemplates.MinOrderPlayers
                && order.Players <= MiyakoTraderPromptTemplates.MaxOrderPlayers
                && order.Level >= MiyakoTraderPromptTemplates.MinOrderLevel
                && order.Level <= MiyakoTraderPromptTemplates.MaxOrderLevel
                && order.Duration >= MiyakoTraderPromptTemplates.MinOrderDuration;
        }

        private static bool ValidateTicket(TicketIntent ticket)
        {
            return ticket != null
                && ticket.Percent >= MiyakoTraderPromptTemplates.MinTicketPercent
                && ticket.Percent <= MiyakoTraderPromptTemplates.MaxTicketPercent;
        }

        private string BuildSpawnTypeHelp()
        {
            var spawnTypes = configService.GetSpawnTypes();
            if (spawnTypes == null || spawnTypes.IsEmpty)
            {
                return "(none)";
            }

            var sb = new StringBuilder();
            foreach (var kvp in spawnTypes.OrderBy(x => x.Key))
            {
                var displayName = string.IsNullOrEmpty(kvp.Value.DisplayName) ? kvp.Value.WildSpawnType : kvp.Value.DisplayName;
                sb.Append("- index ").Append(kvp.Key).Append(" -> ").AppendLine(displayName);
            }
            return sb.ToString();
        }

        private static ILlmProvider CreateProvider(string providerName)
        {
            return providerName switch
            {
                "OpenAICompatible" => new OpenAICompatibleProvider(),
                "Anthropic"        => new AnthropicProvider(),
                "GoogleGemini"     => new GoogleGeminiProvider(),
                "DashScope"        => new DashScopeProvider(),
                "Zhipu"            => new ZhipuProvider(),
                "Qianfan"          => new QianfanProvider(),
                "Spark"            => new SparkProvider(),
                "MiniMax"           => new MiniMaxProvider(),
                _                  => null,
            };
        }

        private sealed class RateBucket
        {
            private readonly int _maxPerMinute;
            private int _consumed;
            private long _windowStartTicks;

            public RateBucket(int maxPerMinute)
            {
                _maxPerMinute = maxPerMinute;
                _windowStartTicks = DateTime.UtcNow.Ticks;
            }

            public bool TryConsume()
            {
                var now = DateTime.UtcNow.Ticks;
                var windowTicks = TimeSpan.FromMinutes(1).Ticks;
                var start = Interlocked.Read(ref _windowStartTicks);
                if (now - start >= windowTicks)
                {
                    // 进入新窗口
                    if (Interlocked.CompareExchange(ref _windowStartTicks, now, start) == start)
                    {
                        Interlocked.Exchange(ref _consumed, 0);
                    }
                }

                var consumed = Interlocked.Increment(ref _consumed);
                return consumed <= _maxPerMinute;
            }
        }
    }

    /// <summary>
    /// LLM 分发结果。<see cref="IsHandled"/> 为 true 表示已处理（无论成功/兜底），
    /// false 时由调用方走原 unknown-command 流程。
    /// </summary>
    public readonly struct LlmDispatchResult
    {
        public bool IsHandled { get; }

        private LlmDispatchResult(bool handled)
        {
            IsHandled = handled;
        }

        public static LlmDispatchResult Handled()
        {
            return new LlmDispatchResult(true);
        }

        public static LlmDispatchResult NotHandled()
        {
            return new LlmDispatchResult(false);
        }
    }
}