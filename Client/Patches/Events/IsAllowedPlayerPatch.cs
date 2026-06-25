using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    public sealed class IsAllowedPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(LighthouseTraderZone), nameof(LighthouseTraderZone.IsAllowedPlayer));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(Player player, List<Player> ___allowedPlayers, ref bool __result)
        {
            if (McsMgr.IsMcsBotPlayer(player.ProfileId))
            {
                var mcsLeadPlayer = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(player.ProfileId);
                __result = ___allowedPlayers.Contains(mcsLeadPlayer);
            }
        }
    }
}