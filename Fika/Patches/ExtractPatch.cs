using System.Reflection;
using EFT.Interactive;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    public class ExtractPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(CoopGame), nameof(CoopGame.Extract));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Prefix(FikaPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null)
        {
            if (!McsMgr.IsHost)
            {
                return;
            }

            if (McsMgr.IsMcsLeadPlayer(player.ProfileId))
            {
                EventMgr.Notify(new McsLeadPlayerExtractedEvent
                {
                    McsLeadPlayerId = player.ProfileId
                });
            }
        }
    }
}