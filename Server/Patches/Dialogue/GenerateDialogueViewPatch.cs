
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.Patches.Dialogue
{
    /// <summary>
    /// 对宫子好友首次获取聊天消息内容时都以商人消息类型发送一条问候语，保证玩家存档中与宫子的聊天类型一定以商人类型创建
    /// </summary>
    public sealed class GenerateDialogueViewPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GenerateDialogueView));

        public GenerateDialogueViewPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static MailSendService MailSendService { get => field ??= ServiceProvider.GetService<MailSendService>(); }
        private static ServerLocalisationService ServerLocalisationService { get => field ??= ServiceProvider.GetService<ServerLocalisationService>(); }
        private static TraderService TraderService { get => field ??= ServiceProvider.GetService<TraderService>(); }
        private static ConfigService ConfigService { get => field ??= ServiceProvider.GetService<ConfigService>(); }

        [PatchPrefix]
        public static void Prefix(ref GetMailDialogViewRequestData request, MongoId sessionId)
        {
            if (request.DialogId == TraderService.MiyakoTraderId)
            {
                MailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERWELCOME,
                    null
                );

                MailSendService.SendDirectNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    string.Format(ServerLocalisationService.GetText(Locales.CURRENTPRICEINCREASE), Math.Round(TraderService.GetGlobalPunishmentMulti() * 100d, 2)),
                    null
                );

                if (ConfigService.GetMcsPluginConfig().ServerConfig.CheckUpdate && ConfigService.HaveUpdate)
                {
                    MailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        string.Format(ServerLocalisationService.GetText(Locales.NEWVERSIONNOTIFY), ConfigService.GetClientVersion(), ConfigService.GetLatestVersion()),
                        null
                    );
                }
                request.Type = MessageType.NpcTraderMessage;
            }
        }
    }
}