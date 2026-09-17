using System.Reflection;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>  
    /// 让护航能识别骨折并进行治疗 
    /// </summary>  
    public sealed class FindDamagedPartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotFirstAid), nameof(BotFirstAid.FindDamagedPart));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(BotFirstAid __instance)
        {
            if (!McsMgr.IsMcsBotPlayer(__instance._owner.ProfileId))
            {
                return;
            }

            ClearUntreatableDestroyedPart(__instance);

            if (__instance.Damaged)
            {
                return;
            }

            if (__instance.CurUsingMeds == null || !__instance.CanHealDamageEffectType(__instance.CurUsingMeds, EDamageEffectType.Fracture))
            {
                return;
            }

            var healthController = __instance._owner.GetPlayer.HealthController;
            var fracture = healthController.FindExistingEffect<IFracture>(EBodyPart.Common);
            if (fracture != null)
            {
                __instance._bodyPartToHeal = fracture.BodyPart;
                __instance.Damaged = true;
            }
        }

        private static void ClearUntreatableDestroyedPart(BotFirstAid __instance)
        {
            if (!__instance.Damaged || __instance._bodyPartToHeal == null || __instance.CurUsingMeds == null)
            {
                return;
            }

            var bodyPart = __instance._bodyPartToHeal.Value;
            var healthController = __instance._owner.GetPlayer.HealthController;
            if (!healthController.IsBodyPartDestroyed(bodyPart))
            {
                return;
            }

            if (__instance.CanHealDamageEffectType(__instance.CurUsingMeds, EDamageEffectType.DestroyedPart))
            {
                return;
            }

            __instance._bodyPartToHeal = null;
            __instance.Damaged = false;
        }
    }
}