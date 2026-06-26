
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 优化原生的拆除绊雷位置寻路
    /// </summary>
    public class DeactivateMinePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DeactivateMineBaseLogic), nameof(DeactivateMineBaseLogic.method_7));

        [PatchPrefix]
        public static bool Prefix(DeactivateMineBaseLogic __instance, Vector3 pos)
        {
            if (__instance.Float_0 < Time.time)
            {
                if (Tools.BetterDestination(1.5f, pos, out var betterDestination))
                {
                    __instance.BotOwner_0.Mover.GoToPoint(betterDestination, false, 0.5f);
                }
                __instance.Float_0 = Time.time + 5f;
            }
            return false;
        }
    }
}