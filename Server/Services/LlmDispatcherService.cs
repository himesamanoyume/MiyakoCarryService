using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.Services.Llm
{
    /// <summary>
    /// 服务端 LLM 商人对话分发器。玩家在与宫子商人对话中输入任意自然语言文本时被调用
    /// </summary>
    [Injectable(InjectionType.Singleton)]
    public class LlmDispatcherService(
        ConfigService configService,
        QuestController questController,
        ProfileService profileService,
        InfoService infoService,
        MailSendService mailSendService,
        ServerLocalisationService serverLocalisationService,
        DialogueHelper dialogueHelper,
        LocaleService localeService,
        TraderService traderService,
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

            if (!serverConfig.TraderLlmEnabled)
            {
                return LlmDispatchResult.NotHandled();
            }

            // 限流：每分钟 LlmMaxMessagesPerMinute 条
            var maxPerMinute = serverConfig.TraderLlmMaxMessagesPerMinute > 0 ? serverConfig.TraderLlmMaxMessagesPerMinute : 10;
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
                ApiKey = serverConfig.TraderLlmApiKey,
                ApiSecret = serverConfig.TraderLlmApiSecret,
                BaseUrl = serverConfig.TraderLlmBaseUrl,
                ModelId = serverConfig.TraderLlmModelId,
                SystemPrompt = Tools.BuildSystemPrompt(serverConfig.TraderLlmSystemPrompt, BuildSpawnTypeHelp(), BuildPricingHelp(), BuildSquadsHelp(sessionId)),
                Temperature = serverConfig.TraderLlmTemperature,
                MaxTokens = serverConfig.TraderLlmMaxTokens,
                TimeoutSec = serverConfig.TraderLlmTimeoutSec,
                ReasoningEffort = serverConfig.TraderLlmReasoningEffort,
            };

            var provider = CreateProvider(serverConfig.TraderLlmProvider);
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

            // 按 HttpProxyHost/HttpProxyPort 应用代理（幂等，仅在配置变化时重建）
            provider.ApplyProxy(serverConfig.HttpProxyHost, serverConfig.HttpProxyPort);

            LlmIntent intent;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(serverConfig.TraderLlmTimeoutSec > 0 ? serverConfig.TraderLlmTimeoutSec : 30));
                intent = await provider.InterpretAsync(BuildHistoryContext(sessionId, text), settings, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Error("LlmDispatcher 异常: " + ex);
                SendLlmErrorDetail(sessionId, ex.Message);
                return LlmDispatchResult.Handled();
            }

            if (intent == null || intent.IsError)
            {
                logger.Warning("LlmDispatcher 意图解析失败: " + (intent?.Error ?? "null"));
                SendLlmErrorDetail(sessionId, intent?.Error ?? "null");
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
                    Locales.MIYAKOTRADERORDERNEWQUEST,
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
                    Locales.MIYAKOTRADERTICKETNEWQUEST,
                    null);

                questController.CreateTicketQuest(sessionId, ticket.Percent);
                return LlmDispatchResult.Handled();
            }

            if (intent.IsCommand && intent.Renew != null)
            {
                var renewAid = ResolveTargetAid(sessionId, intent.Renew.Target);
                if (renewAid == null)
                {
                    mailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        serverLocalisationService.GetText(Locales.MIYAKOTRADERORDERNOTFOUND),
                        null);
                    return LlmDispatchResult.Handled();
                }

                if (questController.RenewOrder(sessionId, renewAid))
                {
                    mailSendService.SendLocalisedNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        Locales.MIYAKOTRADERRENEWSUCCESS,
                        null);
                }
                else
                {
                    mailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        serverLocalisationService.GetText(Locales.MIYAKOTRADEROPERATIONFAILED),
                        null);
                }
                return LlmDispatchResult.Handled();
            }

            if (intent.IsCommand && intent.Settle != null)
            {
                var settleAid = ResolveTargetAid(sessionId, intent.Settle.Target);
                if (settleAid == null)
                {
                    mailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        serverLocalisationService.GetText(Locales.MIYAKOTRADERORDERNOTFOUND),
                        null);
                    return LlmDispatchResult.Handled();
                }

                if (profileService.SettleOrder(sessionId, settleAid))
                {
                    mailSendService.SendLocalisedNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        Locales.MIYAKOTRADERSETTLESUCCESS,
                        null);
                }
                else
                {
                    mailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        serverLocalisationService.GetText(Locales.MIYAKOTRADEROPERATIONFAILED),
                        null);
                }
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
                && order.Players >= Tools.MinOrderPlayers
                && order.Players <= Tools.MaxOrderPlayers
                && order.Level >= Tools.MinOrderLevel
                && order.Level <= Tools.MaxOrderLevel
                && order.Duration >= Tools.MinOrderDuration;
        }

        private static bool ValidateTicket(TicketIntent ticket)
        {
            return ticket != null
                && ticket.Percent >= Tools.MinTicketPercent
                && ticket.Percent <= Tools.MaxTicketPercent;
        }

        /// <summary>
        /// 向玩家发送 AI 错误通知并附带具体错误原因，避免只回复笼统的"AI 不可用"。
        /// </summary>
        private void SendLlmErrorDetail(MongoId sessionId, string reason)
        {
            const int maxReasonLength = 300;
            if (reason != null && reason.Length > maxReasonLength)
            {
                reason = reason.Substring(0, maxReasonLength) + "...";
            }

            mailSendService.SendDirectNpcMessageToPlayer(
                sessionId,
                TraderService.MiyakoTraderId,
                MessageType.NpcTraderMessage,
                string.Format(serverLocalisationService.GetText(Locales.MIYAKOTRADERLLMERRORDETAIL), reason),
                null);
        }

        /// <summary>
        /// 从玩家与宫子商人的聊天记录中构建 LLM 上下文。
        /// 包含玩家消息（老板）、店长回复以及 <c>Mcs/*</c> 事件通知（订单/罚单创建等，以英文解析），
        /// 并附加"仅回应当前消息"的指令，保证对话连贯。条数由 <c>LlmMaxHistoryMessages</c> 控制，0 表示关闭。
        /// </summary>
        private string BuildHistoryContext(MongoId sessionId, string currentText)
        {
            const int maxMessageLength = 300;
            const int maxTotalLength = 6000;

            var maxHistory = configService.GetMcsPluginConfig().ServerConfig.TraderLlmMaxHistoryMessages;
            if (maxHistory <= 0)
            {
                return currentText;
            }

            var dialogue = dialogueHelper.GetDialogsForProfile(sessionId).GetValueOrDefault(TraderService.MiyakoTraderId);
            if (dialogue?.Messages is not { Count: > 0 })
            {
                return currentText;
            }

            var entries = new List<string>();
            foreach (var message in dialogue.Messages)
            {
                // 跳过瞬时提示（正在思考/限流）
                if (message.TemplateId is Locales.MIYAKOTRADERLLMINTERPRETING or Locales.MIYAKOTRADERLLMCOOLDOWN)
                {
                    continue;
                }

                var text = message.Text;
                if (string.IsNullOrEmpty(text))
                {
                    if (message.TemplateId != null && message.TemplateId.StartsWith("Mcs/", StringComparison.Ordinal))
                    {
                        text = localeService.GetGlobalLocalizedText(message.TemplateId);
                    }

                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }
                }

                // 跳过与当前消息重复的玩家消息（刚发送的那条已由 SendPlayerMessageToNpc 写入记录）
                if (message.MessageType == MessageType.UserMessage && string.Equals(text.Trim(), currentText.Trim(), StringComparison.Ordinal))
                {
                    continue;
                }

                if (text.Length > maxMessageLength)
                {
                    text = text.Substring(0, maxMessageLength) + "...";
                }

                var speaker = message.MessageType == MessageType.UserMessage ? "Player" : "MiyakoTrader";
                entries.Add($"{speaker}: {text}");
            }

            if (entries.Count == 0)
            {
                return currentText;
            }

            entries = entries.Skip(Math.Max(0, entries.Count - maxHistory)).ToList();

            var sb = new StringBuilder("Chat history (context only — respond ONLY to the current message, never to the history):");
            var total = 0;
            foreach (var entry in entries)
            {
                if (total + entry.Length > maxTotalLength)
                {
                    break;
                }

                sb.Append('\n').Append(entry);
                total += entry.Length;
            }

            sb.Append("\n\nCurrent message: ").Append(currentText);
            return sb.ToString();
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

        /// <summary>
        /// 构建实时定价数据：当前涨价惩罚、各护航级别基准价格区间、罚单单价。
        /// </summary>
        private string BuildPricingHelp()
        {
            var serverConfig = configService.GetMcsPluginConfig().ServerConfig;
            var punishmentPercent = Math.Round(traderService.GetGlobalPunishmentMulti() * 100d, 2);

            var sb = new StringBuilder();
            sb.Append("- Current 涨价惩罚 (price-increase punishment): ").Append(punishmentPercent).AppendLine("%");
            sb.AppendLine("- Base price per 护航 per hour (rubles, before punishment):");
            foreach (var kvp in serverConfig.CarryServiceLevelPrice.OrderBy(x => x.Key))
            {
                sb.Append("  level ").Append(kvp.Key).Append(": ").Append(kvp.Value.Min).Append(" ~ ").AppendLine(kvp.Value.Max.ToString());
            }
            sb.Append("- 罚单 (ticket) price: ").Append(serverConfig.TicketPricePerPercent).AppendLine(" rubles per percent");
            return sb.ToString();
        }

        /// <summary>
        /// 构建当前护航列表（昵称 + Aid + 订单状态/级别/时长），供 LLM 识别"续订/结算哪个护航"。
        /// </summary>
        private string BuildSquadsHelp(MongoId sessionId)
        {
            try
            {
                var orderInfos = infoService.GetOrderInfos(sessionId);
                if (orderInfos == null || orderInfos.Count == 0)
                {
                    return null;
                }

                var sb = new StringBuilder();
                foreach (var order in orderInfos)
                {
                    if (order.PlayerIds == null)
                    {
                        continue;
                    }
                    foreach (var botId in order.PlayerIds)
                    {
                        var profile = profileService.GetMcsBotPlayerProfile(sessionId, botId);
                        if (profile?.ProfileInfo == null)
                        {
                            continue;
                        }
                        sb.Append("- ").Append(profile.ProfileInfo.Username)
                          .Append(" | aid: ").Append(profile.ProfileInfo.Aid)
                          .Append(" | status: ").Append(order.Status)
                          .Append(" | level: ").Append(order.CarryServiceLevel)
                          .Append(" | duration: ").Append(order.Duration).AppendLine("h");
                    }
                }
                return sb.Length > 0 ? sb.ToString() : null;
            }
            catch (Exception ex)
            {
                logger.Error("构建护航列表失败: " + ex);
                return null;
            }
        }

        /// <summary>
        /// 按玩家说的昵称或 Aid 匹配当前护航，返回其 Aid；未匹配返回 null。
        /// </summary>
        private string ResolveTargetAid(MongoId sessionId, string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return null;
            }

            var orderInfos = infoService.GetOrderInfos(sessionId);
            if (orderInfos == null)
            {
                return null;
            }

            foreach (var order in orderInfos)
            {
                if (order.PlayerIds == null)
                {
                    continue;
                }
                foreach (var botId in order.PlayerIds)
                {
                    var profile = profileService.GetMcsBotPlayerProfile(sessionId, botId);
                    if (profile?.ProfileInfo == null)
                    {
                        continue;
                    }
                    var aid = profile.ProfileInfo.Aid.ToString();
                    if (string.Equals(aid, target, StringComparison.OrdinalIgnoreCase))
                    {
                        return aid;
                    }
                    var nickname = profile.ProfileInfo.Username;
                    if (!string.IsNullOrEmpty(nickname) && nickname.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return aid;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 服务端启动时的 LLM 连通性测试：当 <c>TraderLlmEnabled</c> 与 <c>TraderLlmStartupTest</c> 均开启时自动执行一次，
        /// 成功/失败均输出日志（失败附带原因）。使用最小化 system prompt，token 成本极低。
        /// </summary>
        public async Task TestConnectionAsync()
        {
            var serverConfig = configService.GetMcsPluginConfig().ServerConfig;

            if (!serverConfig.TraderLlmStartupTest)
            {
                logger.Info("LLM 启动测试跳过：TraderLlmStartupTest 未开启");
                return;
            }

            if (!serverConfig.TraderLlmEnabled)
            {
                logger.Info("LLM 启动测试跳过：TraderLlmEnabled 未开启");
                return;
            }

            var provider = CreateProvider(serverConfig.TraderLlmProvider);
            if (provider == null)
            {
                logger.Error($"LLM 启动测试失败：TraderLlmProvider 配置无效（{serverConfig.TraderLlmProvider}）");
                return;
            }

            var settings = new LlmProviderSettings
            {
                ApiKey = serverConfig.TraderLlmApiKey,
                ApiSecret = serverConfig.TraderLlmApiSecret,
                BaseUrl = serverConfig.TraderLlmBaseUrl,
                ModelId = serverConfig.TraderLlmModelId,
                SystemPrompt = "You are a connectivity test. Reply with exactly: {\"replyText\":\"pong\"}",
                Temperature = 0,
                MaxTokens = 512,
                TimeoutSec = serverConfig.TraderLlmTimeoutSec > 0 ? serverConfig.TraderLlmTimeoutSec : 15,
            };

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSec + 5));
                var intent = await provider.InterpretAsync("ping", settings, cts.Token).ConfigureAwait(false);
                if (intent == null || intent.IsError)
                {
                    logger.Error($"LLM 启动测试失败（{serverConfig.TraderLlmProvider}/{settings.ModelId}）：{intent?.Error ?? "null"}");
                    return;
                }

                logger.Success($"LLM 启动测试成功（{serverConfig.TraderLlmProvider}/{settings.ModelId}）：{(string.IsNullOrEmpty(intent.ReplyText) ? "已收到响应" : intent.ReplyText)}（如无需每次启动测试，可在配置 TraderLlmStartupTest 中设为 false）");
            }
            catch (Exception ex)
            {
                logger.Error($"LLM 启动测试异常（{serverConfig.TraderLlmProvider}/{settings.ModelId}）：{ex.Message}");
            }
        }

        private BaseLlmProvider CreateProvider(string providerName)
        {
            return providerName switch
            {
                "OpenAICompatible" => new OpenAICompatibleProvider(),
                "Anthropic" => new AnthropicProvider(),
                "GoogleGemini" => new GoogleGeminiProvider(),
                "DashScope" => new DashScopeProvider(),
                "Zhipu" => new ZhipuProvider(),
                "Qianfan" => new QianfanProvider(),
                "Spark" => new SparkProvider(),
                "MiniMax" => new MiniMaxProvider(),
                _ => null,
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