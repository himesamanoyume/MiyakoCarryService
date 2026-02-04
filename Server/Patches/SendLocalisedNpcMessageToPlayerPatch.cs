
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace MiyakoCarryService.Server.Patches
{
    /// <summary>
    /// 让临时商人消息以宫子商人Id发送
    /// </summary>
    public sealed class SendLocalisedNpcMessageToPlayerPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MailSendService), nameof(MailSendService.SendLocalisedNpcMessageToPlayer));

        [PatchPrefix]
        public static void Prefix(
            MongoId sessionId,
            ref MongoId? trader,
            MessageType messageType,
            string messageLocaleId,
            IEnumerable<Item>? items,
            long? maxStorageTimeSeconds = 172800,
            SystemData? systemData = null,
            MessageContentRagfair? ragfair = null
        )
        {
            if (trader is not null && trader == Services.TraderService.TempOrderTraderId)
            {
                trader = Services.TraderService.MiyakoTraderId;
            }
        }
    }
}