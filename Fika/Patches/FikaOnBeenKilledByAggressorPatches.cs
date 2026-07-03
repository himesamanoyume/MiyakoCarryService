using System.Reflection;
using EFT;
using Fika.Core;
using Fika.Core.Main.Players;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    /// <summary>
    /// 适配副机的护航的击杀经验共享给老板1
    /// </summary>
    public class FikaOnBeenKilledByAggressorPatch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(FikaBot), nameof(FikaBot.OnBeenKilledByAggressor));

        [PatchPostfix]
        public static void Postfix(Player __instance, IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            Tools.HandleSharedExperience(__instance, aggressor, FikaPlugin.Instance.Settings.SharedKillExperience.Value, FikaPlugin.Instance.Settings.SharedBossExperience.Value);
            Tools.HandleSharedQuestCondition(__instance, aggressor, damageInfo, bodyPart, !FikaPlugin.Instance.Settings.EasyKillConditions.Value);
        }
    }

    /// <summary>
    /// 适配副机的护航的击杀经验共享给老板2
    /// </summary>
    public class FikaOnBeenKilledByAggressorPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ObservedPlayer), nameof(ObservedPlayer.OnBeenKilledByAggressor));

        [PatchPostfix]
        public static void Postfix(Player __instance, IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            Tools.HandleSharedExperience(__instance, aggressor, FikaPlugin.Instance.Settings.SharedKillExperience.Value, FikaPlugin.Instance.Settings.SharedBossExperience.Value);
            Tools.HandleSharedQuestCondition(__instance, aggressor, damageInfo, bodyPart, !FikaPlugin.Instance.Settings.EasyKillConditions.Value);
        }
    }
}