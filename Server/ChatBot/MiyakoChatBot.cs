
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Logger;

namespace MiyakoCarryService.Server.ChatBot
{
    [Injectable]
    public class MiyakoChatBot(
        SptLogger<MiyakoChatBot> logger,
        MailSendService mailSendService,
        ServerLocalisationService serverLocalisationService,
        TraderService traderService,
        IEnumerable<MiyakoChatBotCommands> chatCommands
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
                    string.Format(serverLocalisationService.GetText(Locales.MIYAKOTRADERAVAILABLECOMMANDSPREFIX), chatCommand.CommandPrefix),
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
                            string.Format(serverLocalisationService.GetText(Locales.MIYAKOTRADERSUBCOMMAND), subCommand, commandHelp),
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

                await Task.Delay(TimeSpan.FromSeconds(1));

                mailSendService.SendDirectNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    string.Format(serverLocalisationService.GetText(Locales.CURRENTPRICEINCREASE), Math.Round(traderService.GetGlobalPunishmentMulti() * 100d, 2)),
                    null
                );
            }

            return request.DialogId;
        }
    }
}