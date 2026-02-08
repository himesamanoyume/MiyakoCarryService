
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
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
        MailSendService mailSendService,
        OrderQuestController orderQuestController
    ) : ICommand
    {
        [GeneratedRegex(@"^mcs\s+order\s+(\d+)\s+([1-5])\s+(\d+)$")]
        private static partial Regex OrderCommandRegex();

        public string Command
        {
            get
            {
                return "order";
            }
        }

        public string CommandHelp
        {
            get
            {
                return $"mcs {Command}\n下单指令:\nmcs {Command} {{人数}} {{护航级别}} {{时间}}\n护航级别: 1~5\n时长: 整数, 单位为小时\n示例: mcs {Command} 4 5 24";
            }
        }

        public ValueTask<string> PerformAction(UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
        {
            string value = request.DialogId;
            var match = OrderCommandRegex().Match(request.Text);
            if (match.Success)
            {
                int players = int.Parse(match.Groups[1].Value);
                int level = int.Parse(match.Groups[2].Value);
                int duration = int.Parse(match.Groups[3].Value);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId, 
                    TraderService.MiyakoTraderId, 
                    MessageType.NpcTraderMessage, 
                    Locales.MIYAKOTRADERORDERNEWQUEST,
                    null
                );

                orderQuestController.CreateOrderQuest(sessionId, players, level, duration);
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
            return new(value);
        }
    }
}