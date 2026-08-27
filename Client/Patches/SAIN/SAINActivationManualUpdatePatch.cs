using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    /// <summary>
    /// 当护航处于Mcs层接管期间，每帧强制将SAIN的ActiveLayer复位为None，防止SAINLayersActive卡在true导致BotMover.ManualFixedUpdate被SAIN阻断而无法移动
    /// </summary>
    public sealed class SAINActivationManualUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(SAINUtils.SAINActivationClassType, "ManualUpdate");

        [PatchPostfix]
        public static void Postfix(object __instance)
        {
            if (!SAINUtils.IsMcsActivation(__instance))
            {
                return;
            }

            SAINUtils.SetActiveLayerNone(__instance);
        }
    }
}
