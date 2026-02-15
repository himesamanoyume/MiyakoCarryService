
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiyakoCarryService.Client.Patches.BigSurvey;

/// <summary>
/// 修改调查按钮的提醒数值并修改颜色
/// </summary>
internal sealed class NewNewsCountPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.PropertySetter(typeof(MenuTaskBar), nameof(MenuTaskBar.NewNewsCount));

    [PatchPrefix]
    public static bool Prefix(MenuTaskBar __instance, ref int value)
    {
        var menuTaskBarTraverse = Traverse.Create(__instance);
        var newNewsCount = menuTaskBarTraverse.Field<int>("int_5").Value;
        if (newNewsCount == value)
        {
            return false;
        }
        menuTaskBarTraverse.Field("int_5").SetValue(value);
        var _newNewsObject = menuTaskBarTraverse.Field<GameObject>("_newNewsObject").Value;
        _newNewsObject.SetActive(value > 0);
        _newNewsObject.GetComponent<Image>().color = Draw.DarkRed.Rgb;
        var _newNewsLabel = menuTaskBarTraverse.Field<TextMeshProUGUI>("_newNewsLabel").Value;
        _newNewsLabel.text = value.SubstringIfNecessary();
        return false;
    }
}