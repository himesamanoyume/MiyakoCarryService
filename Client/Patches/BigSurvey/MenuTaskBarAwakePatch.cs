
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using SPT.Common.Http;
using SPT.Common.Utils;
using SPT.Custom.Models;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;
using System.Linq;
using BepInEx;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Patches.BigSurvey;

/// <summary>
/// 修改调查按钮的提醒数值并修改颜色
/// </summary>
public sealed class MenuTaskBarAwakePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MenuTaskBar), nameof(MenuTaskBar.Awake));

    private static GameObject _bigSurveyGameObject;
    private static GameObject _mcsBotPlayerInventoryModeGameObject;
    private static GameObject _bigSurveyButton;
    private static AnimatedToggle _animatedToggle;
    private static HoverTooltipArea _hoverTooltipArea;
    private static GameObject _bigSurveyNewInformation;
    private static GameObject _bigSurveyNewNodes;
    private static TextMeshProUGUI _newBigSurveyLabel;
    private static GameObject _tempNewsGameObject;
    private static GameObject _tempBackGameObject;

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
        if (_tempNewsGameObject == null)
        {
            _tempNewsGameObject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/News");
        }

        if (_tempBackGameObject == null)
        {
            _tempBackGameObject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/Spacer/BackToMatchingContainer");
        }

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
                    stringBuilder.Append("EFT版本: ").Append(EFTVersionInfoClass.String_0).Append("\n")
                        .Append("SPT版本: ").Append(Json.Deserialize<VersionResponse>(RequestHandler.GetJson("/singleplayer/settings/version")).Version).Append("\n")
                        .Append("系统: ").Append(SystemInfo.operatingSystem).Append("\n")
                        .Append("CPU: ").Append(SystemInfo.processorType).Append(" (").Append(SystemInfo.processorCount).Append("核心)\n")
                        .Append("内存: ").Append(SystemInfo.systemMemorySize).Append(" MB\n")
                        .Append("显卡: ").Append(" ").Append(SystemInfo.graphicsDeviceName).Append(" (").Append(SystemInfo.graphicsMemorySize).Append(" MB显存)\n")
                        .Append("全部Client模组:\n")
                        .Append(string.Join(", ", Chainloader.PluginInfos.Values.Select(x => x.Instance)
                                .Where(plugin => plugin != null)
                                .Union(Object.FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>()).Select(p => p.Info.Metadata.Name)
                                .ToArray())).Append("\n")
                        .Append("全部Server模组:\n")
                        .Append(string.Join(", ", McsRequestHandler.GetLoadedServerMods().Values.Select(x => x.Name))).Append('\n')
                        .Append("总计异常: ").Append(MiyakoCarryServicePlugin.LogBuffer.GetLogCount).Append("\n");

                    foreach (var logEntry in MiyakoCarryServicePlugin.LogBuffer.GetEntries())
                    {
                        stringBuilder.Append(logEntry.Condition).Append("\n");
                        stringBuilder.Append(logEntry.StackTrace).Append("\n");
                    }

                    GUIUtility.systemCopyBuffer = stringBuilder.ToString();
                    NotificationManagerClass.DisplayMessageNotification($"共捕获到 {MiyakoCarryServicePlugin.LogBuffer.GetLogCount} 个错误，已复制错误日志文本，可直接粘贴并发送到Discord频道 Rabbit1 Gaming 中（注意：此日志会捕获任何报错，并不一定与宫子护航店有关）");
                    NewBigSurveyCount = 0;
                    _animatedToggle.ToggleSilent(false);
                });
            }
            _hoverTooltipArea = _bigSurveyGameObject.GetComponentInChildren<HoverTooltipArea>();
            SetBigSurveyButtonInteractable(true);
        }

        if (_tempBackGameObject != null)
        {
            _mcsBotPlayerInventoryModeGameObject = Object.Instantiate(_tempBackGameObject);
            _mcsBotPlayerInventoryModeGameObject.name = "McsBotPlayerInventoryModeInfo";
            _mcsBotPlayerInventoryModeGameObject.transform.SetParent(_tempBackGameObject.transform.parent, false);
            _mcsBotPlayerInventoryModeGameObject.transform.GetChild(0).gameObject.SetActive(true);
            _mcsBotPlayerInventoryModeGameObject.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
            _mcsBotPlayerInventoryModeGameObject.transform.GetChild(0).GetChild(3).gameObject.SetActive(false);

            var matchingStatus = _mcsBotPlayerInventoryModeGameObject.transform.GetChild(0).GetChild(1);
            matchingStatus.GetComponentInChildren<TextMeshProUGUI>().text = "护航库存模式";
            ShowMcsBotPlayerInventoryModeInfo(false);
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
            newList.Remove(newList[^1]);
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

    public static void ShowMcsBotPlayerInventoryModeInfo(bool active)
    {
        _mcsBotPlayerInventoryModeGameObject.SetActive(active);
    }
}