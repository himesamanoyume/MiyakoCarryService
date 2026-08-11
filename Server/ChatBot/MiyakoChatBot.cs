
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Services.Llm;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.ChatBot
{
    [Injectable]
    public class MiyakoChatBot(
        ISptLogger<MiyakoChatBot> logger,
        MailSendService mailSendService,
        ServerLocalisationService serverLocalisationService,
        ProfileService profileService,
        IEnumerable<MiyakoChatBotCommands> chatCommands,
        LlmDispatcherService llmDispatcher
    ) : IDialogueChatBot
    {
        private static readonly MongoId _miyakoId = new(TraderService.MiyakoTraderId);

        protected readonly IDictionary<string, MiyakoChatBotCommands> _chatCommands = chatCommands.ToDictionary(command => command.CommandPrefix);

        public UserDialogInfo GetChatBot()
        {
            return new UserDialogInfo
            {
                Id = _miyakoId,
                Aid = 1560107,
                Info = new()
                {
                    Level = 15,
                    MemberCategory = MemberCategory.Developer,
                    SelectedMemberCategory = MemberCategory.Developer,
                    Nickname = "Miyako",
                    Side = "Usec",
                },
            };
        }

        public async ValueTask<string> HandleMessage(MongoId sessionId, SendMessageRequest request)
        {
            if (request.Text.Length == 0)
            {
                logger.Error(serverLocalisationService.GetText("chatbot-command_was_empty"));

                return request.DialogId;
            }

            if (profileService.IsMcsBotPlayerInventoryMode(sessionId))
            {
                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERINVENTORYMODEREFUSE,
                    null
                );

                return string.Empty;
            }

            var splitCommand = request.Text.Split(" ");

            if (
                splitCommand.Length > 1
                && _chatCommands.TryGetValue(splitCommand[0], out var commando)
                && commando.Commands.Contains(splitCommand[1])
            )
            {
                return await commando.Handle(splitCommand[1], GetChatBot(), sessionId, request);
            }

            if (string.Equals(splitCommand.FirstOrDefault(), "help", StringComparison.OrdinalIgnoreCase))
            {
                return await SendPlayerHelpMessage(sessionId, request);
            }

            // LLM 兜底：玩家直接用自然语言对话时，由 LlmDispatcher 解释意图并派发到 QuestController 或纯回复。
            // 原 mcs order/mcs ticket 走上面 splitCommand 流程，到达此处即为 free-text；
            // LLM 关闭则 LlmDispatcher 返回 NotHandled，继续走原 unknown-command 流程。
            var llmResult = await llmDispatcher.TryDispatchAsync(sessionId, request.Text);
            if (llmResult.IsHandled)
            {
                return request.DialogId;
            }

            mailSendService.SendLocalisedNpcMessageToPlayer(
                sessionId,
                TraderService.MiyakoTraderId,
                MessageType.NpcTraderMessage,
                Locales.MIYAKOTRADERUNRECOGNIZEDCOMMAND,
                null
            );

            return string.Empty;
        }

        protected async ValueTask<string> SendPlayerHelpMessage(MongoId sessionId, SendMessageRequest request)
        {
            mailSendService.SendLocalisedNpcMessageToPlayer(
                sessionId,
                TraderService.MiyakoTraderId,
                MessageType.NpcTraderMessage,
                Locales.MIYAKOTRADERAVAILABLECOMMANDSLIST,
                null
            );

            foreach (var chatCommand in _chatCommands.Values)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                mailSendService.SendDirectNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    serverLocalisationService.GetText(Locales.MIYAKOTRADERAVAILABLECOMMANDSPREFIX, new { CommandPrefix = chatCommand.CommandPrefix }),
                    null
                );

                foreach (var subCommand in chatCommand.Commands)
                {
                    foreach (var commandHelp in chatCommand.GetCommandHelps(subCommand))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));

                        mailSendService.SendDirectNpcMessageToPlayer(
                            sessionId,
                            TraderService.MiyakoTraderId,
                            MessageType.NpcTraderMessage,
                            serverLocalisationService.GetText(Locales.MIYAKOTRADERSUBCOMMAND, new { SubCommand = subCommand, Help = commandHelp }),
                            null
                        );

                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERSPECIALHELP,
                    null
                );
            }

            return request.DialogId;
        }
    }
}