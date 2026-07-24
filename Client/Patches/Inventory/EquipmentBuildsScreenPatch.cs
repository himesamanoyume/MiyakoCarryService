
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
        public static bool Prefix(EEquipmentBuildType ____currentBuildTabType, EquipmentBuildsStorage ____buildStorage, Tab ____customBuildsTab, Tab ____standardBuildsTab, TabGroup ____buildTypesTabGroup)
        {
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return true;
            }

            if (____buildStorage == null || ____customBuildsTab == null || ____standardBuildsTab == null || ____buildTypesTabGroup == null)
            {
                return true;
            }

            if (____currentBuildTabType == EEquipmentBuildType.Custom && !____buildStorage.HasCustomBuilds)
            {
                ____customBuildsTab.SetInteractable(false);
                ____customBuildsTab.Deselect().HandleExceptions();
                ____buildTypesTabGroup.Show(____standardBuildsTab, true);
                return false;
            }
            return true;
        }
    }
}