
using System.Reflection;
using EFT.Builds;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.Group;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Inventory
{
    /// <summary>  
    /// 修复护航库存模式下使用装备预设的相关问题
    /// </summary>  
    public sealed class EquipmentBuildsScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EquipmentBuildsScreen), nameof(EquipmentBuildsScreen.UpdateBuildList));

        [PatchPrefix]
        public static bool Prefix(EquipmentBuildsScreen __instance)
        {
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return true;
            }

            if (__instance._buildStorage == null || __instance._customBuildsTab == null || __instance._standardBuildsTab == null || __instance._buildTypesTabGroup == null)
            {
                return true;
            }

            if (__instance._currentBuildTabType == EEquipmentBuildType.Custom && !__instance._buildStorage.HasCustomBuilds)
            {
                __instance._customBuildsTab.SetInteractable(false);
                __instance._customBuildsTab.Deselect().HandleExceptions();
                __instance._buildTypesTabGroup.Show(__instance._standardBuildsTab, true);
                return false;
            }
            return true;
        }
    }
}