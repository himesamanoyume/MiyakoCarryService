
using System.Reflection;
using EFT.Builds;
using EFT.UI;
using EFT.UI.Builds;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.Group;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Inventory
{
    /// <summary>  
    /// 修复护航库存模式下使用装备预设的相关问题
    /// </summary>  
    public sealed class EquipmentBuildsScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EquipmentBuildsScreen), nameof(EquipmentBuildsScreen.UpdateBuildList));

        [PatchPrefix]
        public static bool Prefix(EEquipmentBuildType ___eequipmentBuildType_0, EquipmentBuildsStorage ___equipmentBuildsStorage, Tab ____customBuildsTab, Tab ____standardBuildsTab, TabGroup ___tabGroup_1)
        {
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return true;
            }

            if (___equipmentBuildsStorage == null || ____customBuildsTab == null || ____standardBuildsTab == null || ___tabGroup_1 == null)
            {
                return true;
            }

            if (___eequipmentBuildType_0 == EEquipmentBuildType.Custom && !___equipmentBuildsStorage.HasCustomBuilds)
            {
                ____customBuildsTab.SetInteractable(false);
                ____customBuildsTab.Deselect().HandleExceptions();
                ___tabGroup_1.Show(____standardBuildsTab, true);
                return false;
            }
            return true;
        }
    }
}