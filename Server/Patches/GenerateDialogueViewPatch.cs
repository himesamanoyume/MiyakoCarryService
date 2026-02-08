
using System;
using System.Reflection;
using System.Threading;
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

namespace MiyakoCarryService.Server.Patches
{
    /// <summary>
    /// 对宫子好友首次开始聊天时以商人消息类型创建
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
                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId, 
                    TraderService.MiyakoTraderId, 
                    MessageType.NpcTraderMessage, 
                    Locales.MIYAKOTRADERWELCOME,
                    null
                );
                request.Type = MessageType.NpcTraderMessage;
            }
        }
    }
}