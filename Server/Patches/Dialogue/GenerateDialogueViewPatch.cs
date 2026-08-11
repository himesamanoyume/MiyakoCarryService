
using System;
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
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
    [Injectable]
    public sealed class GenerateDialogueViewPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GenerateDialogueView));

        public GenerateDialogueViewPatch(MailSendService mailSendService, ServerLocalisationService serverLocalisationService, TraderService traderService, ConfigService configService)
        {
            _mailSendService = mailSendService;
            _serverLocalisationService = serverLocalisationService;
            _traderService = traderService;
            _configService = configService;
        }

        private static MailSendService _mailSendService;
        private static ServerLocalisationService _serverLocalisationService;
        private static TraderService _traderService;
        private static ConfigService _configService;

        [PatchPrefix]
        public static void Prefix(ref GetMailDialogViewRequestData request, MongoId sessionId)
        {
            if (request.DialogId == TraderService.MiyakoTraderId)
            {
                _mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.MIYAKOTRADERWELCOME,
                    null
                );

                _mailSendService.SendDirectNpcMessageToPlayer(
                    sessionId,
                    TraderService.MiyakoTraderId,
                    MessageType.NpcTraderMessage,
                    _serverLocalisationService.GetText(Locales.CURRENTPRICEINCREASE, new { Percent = Math.Round(_traderService.GetGlobalPunishmentMulti() * 100d, 2) }),
                    null
                );

                if (_configService.GetMcsPluginConfig().ServerConfig.TraderLlmEnabled)
                {
                    _mailSendService.SendLocalisedNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        Locales.MIYAKOTRADERLLMENABLED,
                        null
                    );
                }

                if (_configService.GetMcsPluginConfig().ServerConfig.CheckUpdate && _configService.HaveUpdate)
                {
                    _mailSendService.SendDirectNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        _serverLocalisationService.GetText(Locales.NEWVERSIONNOTIFY, new { CurrentVersion = _configService.GetClientVersion(), LatestVersion = _configService.GetLatestVersion() }),
                        null
                    );
                }

                if (!_traderService.CheckProfileTraderInfo(sessionId))
                {
                    _mailSendService.SendLocalisedNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        _serverLocalisationService.GetText(Locales.INVAILDPROFILETRADERINFOWARNING),
                        null
                    );
                }

                if (!_traderService.CheckServerTraderTable())
                {
                    _mailSendService.SendLocalisedNpcMessageToPlayer(
                        sessionId,
                        TraderService.MiyakoTraderId,
                        MessageType.NpcTraderMessage,
                        _serverLocalisationService.GetText(Locales.INVAILDSERVERTRADERTABLEWARNING),
                        null
                    );
                }

                request.Type = MessageType.NpcTraderMessage;
            }
        }
    }
}