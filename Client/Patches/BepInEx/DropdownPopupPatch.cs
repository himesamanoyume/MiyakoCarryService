using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.BepInEx
{
    /// <summary>
    /// 在 ConfigurationManager 窗口内容绘制完成后悬浮绘制枚举下拉框的选项弹层，
    /// 复刻原生 AcceptableValueList 的弹层行为（不挤占布局高度）。
    /// </summary>
    public sealed class DropdownPopupPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ConfigurationManager.ConfigurationManager), "SettingsWindow", [typeof(int)]);

        [PatchPostfix]
        public static void Postfix()
        {
            MiyakoCarryServicePlugin.DrawDropdownPopup();
        }
    }
}
