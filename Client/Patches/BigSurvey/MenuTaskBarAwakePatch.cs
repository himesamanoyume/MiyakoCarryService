
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using SPT.Common.Http;
using SPT.Common.Utils;
using SPT.Custom.Models;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.BigSurvey;

/// <summary>
/// 修改调查按钮的提醒数值并修改颜色
/// </summary>
internal sealed class MenuTaskBarAwakePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MenuTaskBar), nameof(MenuTaskBar.Awake));

    private static GameObject _bigSurveyGameObject;
    private static GameObject _bigSurveyButton;
    private static AnimatedToggle _animatedToggle;
    private static HoverTooltipArea _hoverTooltipArea;
    private static GameObject _bigSurveyNewInformation;
    private static GameObject _bigSurveyNewNodes;
    private static TextMeshProUGUI _newBigSurveyLabel;
    private static GameObject _tempNewsGameObject;

    public static int NewBigSurveyCount
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            if (_bigSurveyNewInformation != null)
            {
                _bigSurveyNewInformation.SetActive(value > 0);
            }
            if (_bigSurveyNewNodes != null)
            {
                _bigSurveyNewNodes.SetActive(value > 0);
            }
            if (_newBigSurveyLabel != null)
            {
                _newBigSurveyLabel.text = value.SubstringIfNecessary();
            }
        }
    } = 0;

    [PatchPrefix]
    [HarmonyPriority(Priority.First)]
    public static void Prefix(MenuTaskBar __instance)
    {
        _tempNewsGameObject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/News");
        if (_tempNewsGameObject != null)
        {
            _bigSurveyGameObject = Object.Instantiate(_tempNewsGameObject);
            _bigSurveyGameObject.name = "BigSurvey";
            _bigSurveyGameObject.transform.SetParent(_tempNewsGameObject.transform.parent, false);
            _bigSurveyGameObject.transform.SetSiblingIndex(10);

            _bigSurveyButton = _bigSurveyGameObject.transform.GetChild(0).gameObject;
            _bigSurveyButton.name = "BigSurveyButton";
            _bigSurveyButton.GetComponentInChildren<LocalizedText>().LocalizationKey = "获取日志";

            _bigSurveyNewInformation = _bigSurveyGameObject.transform.GetChild(1).gameObject;
            _bigSurveyNewNodes = _bigSurveyNewInformation.transform.GetChild(0).gameObject;
            _newBigSurveyLabel = _bigSurveyNewNodes.transform.GetComponentInChildren<TextMeshProUGUI>();

            _animatedToggle = _bigSurveyGameObject.GetComponentInChildren<AnimatedToggle>();
            if (_animatedToggle != null)
            {
                _animatedToggle.onValueChanged.AddListener((arg) =>
                {
                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonBottomBarClick);
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append("Mcs 版本: ").Append(MiyakoCarryServicePlugin.ClientVersion).Append("\n");
                    stringBuilder.Append("EFT版本: ").Append(Class1123.String_0).Append("\n")
                        .Append("SPT版本: ").Append(Json.Deserialize<VersionResponse>(RequestHandler.GetJson("/singleplayer/settings/version")).Version).Append("\n")
                        .Append("系统: ").Append(SystemInfo.operatingSystem).Append("\n")
                        .Append("CPU: ").Append(SystemInfo.processorType).Append(" (").Append(SystemInfo.processorCount).Append("核心)\n")
                        .Append("内存: ").Append(SystemInfo.systemMemorySize).Append(" MB\n")
                        .Append("显卡: ").Append(" ").Append(SystemInfo.graphicsDeviceName).Append(" (").Append(SystemInfo.graphicsMemorySize).Append(" MB显存)\n")
                        .Append("总计异常: ").Append(MiyakoCarryServicePlugin.LogBuffer.GetLogCount).Append("\n");

                    foreach (var logEntry in MiyakoCarryServicePlugin.LogBuffer.GetEntries())
                    {
                        stringBuilder.Append(logEntry.Condition).Append("\n");
                        stringBuilder.Append(logEntry.StackTrace);
                    }

                    GUIUtility.systemCopyBuffer = stringBuilder.ToString();
                    NotificationManagerClass.DisplayMessageNotification($"共捕获到 {MiyakoCarryServicePlugin.LogBuffer.GetLogCount} 个错误，已复制错误日志文本，请直接粘贴并发送到Discord中");
                    NewBigSurveyCount = 0;
                    _animatedToggle.ToggleSilent(false);
                });
            }
            _hoverTooltipArea = _bigSurveyGameObject.GetComponentInChildren<HoverTooltipArea>();
            SetBigSurveyButtonInteractable(true);
        }
    }

    [PatchPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(Dictionary<EMenuType, AnimatedToggle> ____toggleButtons, Dictionary<EMenuType, HoverTooltipArea> ____hoverTooltipAreas, ref GameObject[] ____newInformation)
    {
        if (_tempNewsGameObject != null)
        {
            ____toggleButtons.Remove(EMenuType.NewsHub);
            ____hoverTooltipAreas.Remove(EMenuType.NewsHub);
            Object.Destroy(_tempNewsGameObject.gameObject);
            List<GameObject> newList = [.. ____newInformation];
            if (newList.Count > 0)
            {
                newList.RemoveAt(newList.Count - 1);
            }
            ____newInformation = [.. newList];
        }
    }

    public static void SetBigSurveyButtonInteractable(bool interactable, string customTooltip = null)
    {
        var text = customTooltip ?? "Not available in raid";
        if (_bigSurveyNewInformation != null)
        {
            _bigSurveyNewInformation.SetActive(interactable);
        }
        if (_hoverTooltipArea != null)
        {
            _hoverTooltipArea.SetUnlockStatus(interactable);
            _hoverTooltipArea.SetMessageText(interactable ? string.Empty : text);
        }
    }
}