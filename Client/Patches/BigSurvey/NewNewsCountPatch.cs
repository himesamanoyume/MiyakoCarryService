using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.BigSurvey
{
    /// <summary>
    /// 修改调查按钮的提醒数值并修改颜色
    /// </summary>
    public sealed class NewNewsCountPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(MenuTaskBar), nameof(MenuTaskBar.NewNewsCount));

        [PatchPrefix]
        public static bool Prefix(MenuTaskBar __instance, ref int value)
        {
            var newNewsCount = __instance._newNewsCount;
            if (newNewsCount == value)
            {
                return false;
            }
            __instance._newNewsCount = value;

            var newNewsObject = __instance._newNewsObject;
            if (newNewsObject != null)
            {
                newNewsObject.SetActive(value > 0);
            }

            var newNewsLabel = __instance._newNewsLabel;
            if (newNewsLabel != null)
            {
                newNewsLabel.text = value.SubstringIfNecessary();
            }

            return false;
        }
    }
}