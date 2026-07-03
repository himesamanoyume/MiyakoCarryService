using System.Reflection;
using EFT.Interactive;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using HarmonyLib;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    /// <summary>
    /// 充当玩家撤离时的事件通知作用
    /// </summary>
    public class ExtractPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(CoopGame), nameof(CoopGame.Extract));

        private static McsMgr McsMgr => McsMgrApi.GetMgr<McsMgr>();

        [PatchPrefix]
        public static void Prefix(FikaPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null)
        {
            McsEventApi.Notify(new McsLeadPlayerExtractedEvent
            {
                McsLeadPlayerId = player.ProfileId
            });
        }
    }
}