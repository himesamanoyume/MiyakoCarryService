
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using MiyakoCarryService.Server.Interface;
using MiyakoCarryService.Server.Models.Enums;
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
    public partial class OrderCommand(
        ServerLocalisationService serverLocalisationService,
        MailSendService mailSendService,
        OrderQuestController orderQuestController
    ) : IMcsCommand
    {
        [GeneratedRegex(@"^mcs\s+order\s+([1-4])\s+(0|[1-9]|1\d|20)\s+([1-4])\s+([1-9]\d*)$")]
        private static partial Regex OrderCommandRegex();

        public string Command
        {
            get
            {
                return "order";
            }
        }

        public string[] CommandHelps
        {
            get
            {
                return [
                    string.Format(serverLocalisationService.GetText(Locales.MIYAKOTRADERCOMMANDHELP1), Command, Command, Command), 
                    string.Format(serverLocalisationService.GetText(Locales.MIYAKOTRADERCOMMANDHELP2), serverLocalisationService.GetText(Locales.BOTTYPECOMMON))
                    ];
            }
        }

        public ValueTask<string> PerformAction(UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
        {
            var dialogId = request.DialogId;
            var match = OrderCommandRegex().Match(request.Text);
            if (match.Success)
            {
                var players = int.Parse(match.Groups[1].Value);
                var intBotType = int.Parse(match.Groups[2].Value);
                var botType = Enum.IsDefined(typeof(EBotType), intBotType) ? (EBotType)intBotType : EBotType.common;
                var level = int.Parse(match.Groups[3].Value);
                var duration = int.Parse(match.Groups[4].Value);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId, 
                    TraderService.MiyakoTraderId, 
                    MessageType.NpcTraderMessage, 
                    Locales.MIYAKOTRADERORDERNEWQUEST,
                    null
                );

                orderQuestController.CreateOrderQuest(sessionId, players, botType, level, duration);
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