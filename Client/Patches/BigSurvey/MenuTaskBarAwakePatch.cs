
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
using MiyakoCarryService.Client.Patches.Group;
using UnityEngine.UI;
using MiyakoCarryService.Client.Extensions;
using System;

namespace MiyakoCarryService.Client.Patches.BigSurvey;

/// <summary>
/// 收集错误信息，可用于反馈
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
            _bigSurveyGameObject = UnityEngine.Object.Instantiate(_tempNewsGameObject);
            _bigSurveyGameObject.name = "BigSurvey";
            _bigSurveyGameObject.transform.SetParent(_tempNewsGameObject.transform.parent, false);
            _bigSurveyGameObject.transform.SetSiblingIndex(10);

            _bigSurveyButton = _bigSurveyGameObject.transform.GetChild(0).gameObject;
            _bigSurveyButton.name = "BigSurveyButton";
            _bigSurveyButton.GetComponentInChildren<LocalizedText>().LocalizationKey = Locales.BIGSURVEY;

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
                    stringBuilder.Append("- DateTime: ").Append(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")).Append("\n");
                    stringBuilder.Append("- Mcs Version: ").Append(MiyakoCarryServicePlugin.ClientVersion).Append("\n");
                    stringBuilder.Append("- EFT Version: ").Append(EFTVersionInfoClass.String_0).Append("\n")
                        .Append("- SPT Version: ").Append(Json.Deserialize<VersionResponse>(RequestHandler.GetJson("/singleplayer/settings/version")).Version).Append("\n")
                        .Append("- System: ").Append(SystemInfo.operatingSystem).Append("\n")
                        .Append("- CPU: ").Append(SystemInfo.processorType).Append(" (").Append(SystemInfo.processorCount).Append("Core)\n")
                        .Append("- Memory: ").Append(SystemInfo.systemMemorySize).Append(" MB\n")
                        .Append("- GPU: ").Append(" ").Append(SystemInfo.graphicsDeviceName).Append(" (").Append(SystemInfo.graphicsMemorySize).Append(" MB Memroy)\n")
                        .Append("- All Client Mod:\n")
                        .Append(string.Join(", ", Chainloader.PluginInfos.Values.Select(x => x.Instance)
                                .Where(plugin => plugin != null)
                                .Union(UnityEngine.Object.FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>()).Select(p => $"{p.Info.Metadata.Name}({p.Info.Metadata.Version})")
                                .ToArray())).Append("\n")
                        .Append("- All Server Mod:\n")
                        .Append(string.Join(", ", McsRequestHandler.GetLoadedServerMods().Values.Select(x => $"{x.Name}({x.Version})"))).Append('\n')
                        .Append("Used Layer: ").Append('\n').Append(string.Join(", ", MiyakoCarryServicePlugin.LogBuffer.GetUsedLayers().Select(kvp => $"{kvp.Key}({kvp.Value})"))).Append('\n')
                        .Append("Used Reason: ").Append('\n').Append(string.Join(", ", MiyakoCarryServicePlugin.LogBuffer.GetUsedReasons().Select(kvp => $"{kvp.Key}({kvp.Value})"))).Append('\n')
                        .Append("- Total Exception: ").Append(MiyakoCarryServicePlugin.LogBuffer.GetLogCount).Append("\n");

                    stringBuilder.Append("```log\n");
                    foreach (var logEntry in MiyakoCarryServicePlugin.LogBuffer.GetEntries())
                    {
                        stringBuilder.Append(logEntry.Condition).Append("\n");
                        stringBuilder.Append(logEntry.StackTrace).Append("\n");
                    }
                    stringBuilder.Append("```");

                    GUIUtility.systemCopyBuffer = stringBuilder.ToString();
                    NotificationManagerClass.DisplayMessageNotification(string.Format(Locales.BIGSURVEYNOTIFY.McsLocalized(), MiyakoCarryServicePlugin.LogBuffer.GetLogCount));
                    NewBigSurveyCount = 0;
                    _animatedToggle.ToggleSilent(false);
                });
            }
            _hoverTooltipArea = _bigSurveyGameObject.GetComponentInChildren<HoverTooltipArea>();
            SetBigSurveyButtonInteractable(true);
        }
        ShowMcsBotPlayerInventoryModeInfo(false);
    }

    [PatchPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(Dictionary<EMenuType, AnimatedToggle> ____toggleButtons, Dictionary<EMenuType, HoverTooltipArea> ____hoverTooltipAreas, ref GameObject[] ____newInformation)
    {
        if (_tempNewsGameObject != null)
        {
            ____toggleButtons.Remove(EMenuType.NewsHub);
            ____hoverTooltipAreas.Remove(EMenuType.NewsHub);
            UnityEngine.Object.Destroy(_tempNewsGameObject.gameObject);
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

    private static void InstantiateMcsBotPlayerInventoryModeInfo()
    {
        if (_tempBackGameObject != null && _mcsBotPlayerInventoryModeGameObject == null)
        {
            _mcsBotPlayerInventoryModeGameObject = UnityEngine.Object.Instantiate(_tempBackGameObject);
            _mcsBotPlayerInventoryModeGameObject.name = "McsBotPlayerInventoryModeInfo";
            _mcsBotPlayerInventoryModeGameObject.transform.SetParent(_tempBackGameObject.transform.parent, false);
            _mcsBotPlayerInventoryModeGameObject.transform.GetChild(0).gameObject.SetActive(true);
            _mcsBotPlayerInventoryModeGameObject.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
            _mcsBotPlayerInventoryModeGameObject.transform.GetChild(0).GetChild(3).gameObject.SetActive(false);

            var matchingStatus = _mcsBotPlayerInventoryModeGameObject.transform.GetChild(0).GetChild(1);
            var textMeshProUGUI = matchingStatus.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
            {
                var localizedText = textMeshProUGUI.gameObject.AddComponent<LocalizedText>();
                localizedText.LocalizationKey = Locales.MCSINVENTORYMODE;
            }

            var button = _mcsBotPlayerInventoryModeGameObject.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    GetContextInteractionsPatch.OnExitMcsBotPlayerInventoryMode(GetContextInteractionsPatch.McsBotPlayerAid);
                });
            }
        }
    }

    public static void ShowMcsBotPlayerInventoryModeInfo(bool active)
    {
        InstantiateMcsBotPlayerInventoryModeInfo();
        if (_mcsBotPlayerInventoryModeGameObject != null)
        {
            _mcsBotPlayerInventoryModeGameObject.SetActive(active);
        }
    }
}