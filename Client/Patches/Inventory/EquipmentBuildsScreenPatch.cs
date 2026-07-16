
using System.Reflection;
using EFT.Builds;
using EFT.UI;
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EquipmentBuildsScreen), nameof(EquipmentBuildsScreen.method_5));

        [PatchPrefix]
        public static bool Prefix(EEquipmentBuildType ___eequipmentBuildType_0, EquipmentBuildsStorageClass ___equipmentBuildsStorageClass, Tab ____customBuildsTab, Tab ____standardBuildsTab, GClass3808 ___gclass3808_1)
        {
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return true;
            }

            if (___equipmentBuildsStorageClass == null || ____customBuildsTab == null || ____standardBuildsTab == null || ___gclass3808_1 == null)
            {
                return true;
            }

            if (___eequipmentBuildType_0 == EEquipmentBuildType.Custom && !___equipmentBuildsStorageClass.HasCustomBuilds)
            {
                ____customBuildsTab.vmethod_0(false);
                ____customBuildsTab.Deselect().HandleExceptions();
                ___gclass3808_1.Show(____standardBuildsTab, true);
                return false;
            }
            return true;
        }
    }
}