
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 让护航使用更好的医疗品刷新算法
    /// </summary>
    public sealed class RefreshMedsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotFirstAid), nameof(BotFirstAid.RefreshMeds));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(BotFirstAid __instance)
        {
            if (McsMgr.IsMcsBotPlayer(__instance.botOwner_0.ProfileId))
            {
                __instance.McsRefreshMeds();
                return false;
            }
            return true;
        }
    }
}