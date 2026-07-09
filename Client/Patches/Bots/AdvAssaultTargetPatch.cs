
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 阻止此Layer的启用，时常发生在护航击杀敌人后，会使护航像傻逼一样从敌人面前跑开
    /// </summary>
    public sealed class AdvAssaultTargetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(AdvAssaultTargetLayer), nameof(AdvAssaultTargetLayer.ShallUseNow));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(AdvAssaultTargetLayer __instance, ref bool __result)
        {
            if (McsMgr.IsMcsBotPlayer(__instance.botOwner_0.ProfileId))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}