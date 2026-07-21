using System.Reflection;
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

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(BotFirstAid __instance)
        {
            if (!McsMgr.IsMcsBotPlayer(__instance.botOwner_0.ProfileId))
            {
                return;
            }

            if (__instance.Damaged)
            {
                return;
            }

            if (__instance.CurUsingMeds == null || !__instance.CanHealDamageEffectType(__instance.CurUsingMeds, EDamageEffectType.Fracture))
            {
                return;
            }

            var healthController = __instance.botOwner_0.GetPlayer.HealthController;
            var fracture = healthController.FindExistingEffect<FractureEffect>(EBodyPart.Common);
            if (fracture != null)
            {
                __instance.nullable_0 = fracture.BodyPart;
                __instance.Damaged = true;
            }
        }
    }
}