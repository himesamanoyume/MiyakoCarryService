
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 这个函数总是会自己切换至主武器，烦死了，遂进行阻止
    /// </summary>
    public sealed class BotWeaponSelectorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotWeaponSelector), nameof(BotWeaponSelector.ManualUpdate));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(BotWeaponSelector __instance)
        {
            if (McsMgr.IsMcsBotPlayer(__instance.botOwner_0.ProfileId))
            {
                if (!__instance._errorStuckLog && __instance._startChangeTime > 0f && Time.time - __instance._startChangeTime > 20f)
                {
                    __instance._errorStuckLog = true;
                }
                return false;
            }
            return true;
        }
    }
}