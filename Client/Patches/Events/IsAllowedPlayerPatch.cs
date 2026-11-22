using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    public sealed class IsAllowedPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(LighthouseTraderZone), nameof(LighthouseTraderZone.IsAllowedPlayer));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(LighthouseTraderZone __instance, Player player, ref bool __result)
        {
            if (McsMgr.IsMcsBotPlayer(player.ProfileId))
            {
                var mcsLeadPlayer = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(player.ProfileId);
                __result = __instance.allowedPlayers.Contains(mcsLeadPlayer);
            }
        }
    }
}