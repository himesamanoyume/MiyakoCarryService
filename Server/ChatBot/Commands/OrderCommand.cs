
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
    public partial class OrderCommand(
        ServerLocalisationService serverLocalisationService,
        MailSendService mailSendService,
        OrderQuestController orderQuestController,
        TraderService traderService,
        ConfigService configService
    ) : IMcsCommand
    {
        [GeneratedRegex(@"^mcs\s+order\s+([1-4])\s+(0|[1-9]\d*)\s+([1-5])\s+([1-9]\d*)$")]
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
                var completionConfig = configService.GetOrderConfig().OrderQuests.First().QuestConfig.CompletionConfig.First();
                var punishmentMulti = traderService.GetGlobalPunishmentMulti();

                var sb = new StringBuilder();
                foreach (var kvp in configService.GetSpawnTypes())
                {
                    sb.Append(kvp.Key).Append(": ").Append(kvp.Value.IsBoss ? "[Boss] " : "").Append(serverLocalisationService.GetText(kvp.Value.DisplayName)).Append('\n');
                }

                return [
                    string.Format(
                        serverLocalisationService.GetText(Locales.MIYAKOTRADERCOMMANDHELP1), 
                        Command, 
                        Command, 
                        Command
                        ), 
                    string.Format(
                        serverLocalisationService.GetText(Locales.MIYAKOTRADERCOMMANDHELP2), 
                        (int)(completionConfig.RequestedItemCount.Max * .8f * (1 + punishmentMulti)), 
                        (int)(completionConfig.RequestedItemCount.Max * .85f * (1 + punishmentMulti)), 
                        (int)(completionConfig.RequestedItemCount.Max * .9f * (1 + punishmentMulti)), 
                        (int)(completionConfig.RequestedItemCount.Max * .95f * (1 + punishmentMulti)), 
                        (int)(completionConfig.RequestedItemCount.Max * (1 + punishmentMulti))
                        ),
                    serverLocalisationService.GetText(Locales.MIYAKOTRADERCOMMANDHELP3) + sb.ToString(),
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
                var spawnType = configService.TryGetSpawnType(intBotType);
                var level = int.Parse(match.Groups[3].Value);
                var duration = int.Parse(match.Groups[4].Value);

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId, 
                    TraderService.MiyakoTraderId, 
                    MessageType.NpcTraderMessage, 
                    Locales.MIYAKOTRADERORDERNEWQUEST,
                    null
                );

                orderQuestController.CreateOrderQuest(sessionId, players, spawnType, level, duration);
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