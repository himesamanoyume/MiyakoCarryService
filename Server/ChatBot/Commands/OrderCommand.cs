
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
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

                mailSendService.SendUserMessageToPlayer(sessionId, commandHandler,
                    $"已成功下单！您的订单信息: \n护航{players}人, 护航{level}级, 时长{duration}小时\n请到商人界面接取订单并付款~"
                );

                orderQuestController.CreateOrderQuest(sessionId, players, level, duration);
            }
            else
            {
                mailSendService.SendUserMessageToPlayer(sessionId, commandHandler,
                    "指令错误"
                );
            }
            return new(value);
        }
    }
}