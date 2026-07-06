
using System.Reflection;
using EFT.Builds;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.Group;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Inventory
{
    /// <summary>
    /// 使护航库存模式下点击套装列表时不会立即返回
    /// </summary>
    public sealed class EquipmentBuildsScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EquipmentBuildsScreen), nameof(EquipmentBuildsScreen.Show), [typeof(EquipmentBuildsScreen.GClass3870)]);

        [PatchPrefix]
        public static void Prefix(EquipmentBuildsScreen.GClass3870 controller)
        {
            if (GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                controller.LastEquipmentBuildType = EEquipmentBuildType.Standard;
            }
        }
    }
}