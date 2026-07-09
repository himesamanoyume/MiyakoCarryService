
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 此函数也会在最后尝试切换其他武器，遂进行阻止
    /// </summary>
    public sealed class TryReloadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotReload), nameof(BotReload.TryReload));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(BotReload __instance)
        {
            if (McsMgr.IsMcsBotPlayer(__instance.botOwner_0.ProfileId))
            {
                __instance.McsTryReload();
                return false;
            }
            return true;
        }
    }
}