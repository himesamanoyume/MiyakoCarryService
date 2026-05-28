
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Interface;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace MiyakoCarryService.Server.ChatBot.Commands
{
    [Injectable]
    public partial class TicketCommand(
        ServerLocalisationService serverLocalisationService,
        MailSendService mailSendService,
        QuestController orderQuestController,
        TraderService traderService
    ) : IMcsCommand
    {
        [GeneratedRegex(@"^mcs\s+ticket\s+(100|[1-9]\d?)$")]
        private static partial Regex OrderCommandRegex();

        public string Command
        {
            get
            {
                return "ticket";
            }
        }

        public string[] CommandHelps
        {
            get
            {
                return [
                    string.Format(
                        serverLocalisationService.GetText(Locales.MIYAKOTRADERTICKETCOMMANDHELP1), 
                        Command, 
                        Command, 
                        Command
                        ), 
                    string.Format(
                        serverLocalisationService.GetText(Locales.MIYAKOTRADERTICKETCOMMANDHELP2), 
                        TraderService.TicketPricePerPercent
                )];
            }
        }

        public ValueTask<string> PerformAction(UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
        {
            var dialogId = request.DialogId;
            var match = OrderCommandRegex().Match(request.Text);
            if (match.Success)
            {
                int percent = int.Parse(match.Groups[1].Value);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId, 
                    TraderService.MiyakoTraderId, 
                    MessageType.NpcTraderMessage, 
                    Locales.MIYAKOTRADERTICKETNEWQUEST,
                    null
                );

                var punishmentMulti = traderService.GetGlobalPunishmentMulti();
                orderQuestController.CreateTicketQuest(sessionId, (int)Math.Ceiling(Math.Min(percent, punishmentMulti * 100)));
            }
            else
            {
                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId, 
                    TraderService.MiyakoTraderId, 
                    MessageType.NpcTraderMessage, 
                    Locales.MIYAKOTRADERCOMMANDERROR,
                    null
                );
            }
            return new(dialogId);
        }
    }
}