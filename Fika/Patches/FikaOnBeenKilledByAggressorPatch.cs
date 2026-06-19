using System.Reflection;
using EFT;
using Fika.Core;
using Fika.Core.Main.Players;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.Events;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    /// <summary>
    /// 适配副机的护航的击杀经验共享给老板
    /// </summary>
    public class FikaOnBeenKilledByAggressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ObservedPlayer), nameof(ObservedPlayer.OnBeenKilledByAggressor));

        [PatchPostfix]
        public static void Postfix(Player __instance, IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            OnBeenKilledByAggressorPatch.HandleSharedExperience(__instance, aggressor, FikaPlugin.Instance.Settings.SharedKillExperience.Value, FikaPlugin.Instance.Settings.SharedBossExperience.Value);
        }
    }
}