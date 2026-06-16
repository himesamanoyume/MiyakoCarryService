
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace MiyakoCarryService.Server.Patches.Dialogue
{
    /// <summary>
    /// 对宫子好友首次获取聊天消息内容时都以商人消息类型发送一条问候语，保证玩家存档中与宫子的聊天类型一定以商人类型创建
    /// </summary>
    public sealed class GenerateDialogueViewPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GenerateDialogueView));

        [PatchPrefix]
        public static void Prefix(ref GetMailDialogViewRequestData request, MongoId sessionId)
        {
            if (request.DialogId == TraderService.MiyakoTraderId)
            {
                var mailSendService = ServiceLocator.ServiceProvider.GetService<MailSendService>();
                var serverLocalisationService = ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>();
                var traderService = ServiceLocator.ServiceProvider.GetService<TraderService>();
                var configService = ServiceLocator.ServiceProvider.GetService<ConfigService>();

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId, 
                    TraderService.MiyakoTraderId, 
                    MessageType.NpcTraderMessage, 
                    Locales.MIYAKOTRADERWELCOME,
                    null
                );

                mailSendService.SendDirectNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    string.Format(serverLocalisationService.GetText(Locales.CURRENTPRICEINCREASE), Math.Round(traderService.GetGlobalPunishmentMulti() * 100d, 2)),
                    null
                );

                if (configService.GetMiyakoCarryServiceConfig().ServerConfig.CheckUpdate && configService.HaveUpdate)
                {
                    mailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        string.Format(serverLocalisationService.GetText(Locales.NEWVERSIONNOTIFY), configService.GetClientVersion(), configService.GetLatestVersion()),
                        null
                    );
                }
                request.Type = MessageType.NpcTraderMessage;
            }
        }
    }
}