using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using SPTushonka.Common.Http;
using SPTushonka.Custom.Models;
using SPTushonka.Reflection.Patching;
using TMPro;
using UnityEngine;
using System.Linq;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Client.Patches.Group;
using MiyakoCarryService.Client.Extensions;
using System;
using EFT.Communications;
using UnityEngine.UI;
using MiyakoCarryService.Client.Extensions.ToIl2Cpp;
using BepInEx.Unity.IL2CPP;
using System.Text.Json;
using Il2CppSystem.Linq;
using EFT.Trading;

namespace MiyakoCarryService.Client.Patches.BigSurvey
{
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
                RefreshBigSurveyBadge();
            }
        } = 0;

        private static void RefreshBigSurveyBadge()
        {
            if (_bigSurveyNewInformation != null)
            {
                _bigSurveyNewInformation.SetActive(NewBigSurveyCount > 0);
            }
            if (_bigSurveyNewNodes != null)
            {
                _bigSurveyNewNodes.SetActive(NewBigSurveyCount > 0);
            }
            if (_newBigSurveyLabel != null)
            {
                _newBigSurveyLabel.text = NewBigSurveyCount.SubstringIfNecessary();
            }
        }

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
                var newInformationImage = _bigSurveyNewNodes.GetComponent<Image>();
                if (newInformationImage != null)
                {
                    newInformationImage.color = Draw.DarkRed.Rgb;
                }

                _animatedToggle = _bigSurveyGameObject.GetComponentInChildren<AnimatedToggle>();
                if (_animatedToggle != null)
                {
                    _animatedToggle.onValueChanged.AddListener(new System.Action<bool>((arg) =>
                    {
                        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonBottomBarClick);
                        var stringBuilder = new StringBuilder();
                        stringBuilder.Append("```log\n");
                        stringBuilder.Append("- DateTime: ").Append(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")).Append("\n");
                        stringBuilder.Append("- Mcs Version: ").Append(MiyakoCarryServicePlugin.ClientVersion).Append("\n");
                        stringBuilder.Append("- EFT Version: ").Append(EFT.Version.Current.ToString()).Append("\n")
                            .Append("- SPT Version: ").Append(JsonSerializer.Deserialize<VersionResponse>(RequestHandler.GetJson("/singleplayer/settings/version"))?.Version).Append("\n")
                            .Append("- System: ").Append(SystemInfo.operatingSystem).Append("\n")
                            .Append("- CPU: ").Append(SystemInfo.processorType).Append(" (").Append(SystemInfo.processorCount).Append("Core)\n")
                            .Append("- Memory: ").Append(SystemInfo.systemMemorySize).Append(" MB\n")
                            .Append("- GPU: ").Append(" ").Append(SystemInfo.graphicsDeviceName).Append(" (").Append(SystemInfo.graphicsMemorySize).Append(" MB Memroy)\n")
                            .Append("- All Client Mod:\n")
                            .Append(string.Join(", ", IL2CPPChainloader.Instance.Plugins.Values
                                    .Select(x => $"{x.Metadata.Name}({x.Metadata.Version})")
                                    .ToArray())).Append("\n")
                            .Append("- All Server Mod:\n")
                            .Append(string.Join(", ", McsRequestHandler.GetLoadedServerMods().Values.Select(x => $"{x.Name}({x.Version})"))).Append('\n')
                            .Append(BuildRaidUsedInfoSections()).Append('\n')
                            .Append("- Vaild Trader Info: ").Append(System.Linq.Enumerable.Any<Trader>(GameLoop.Instance.Session.Traders, new Func<Trader, bool>(t => t.Id == MiyakoCarryServicePlugin.MiyakoTraderId))).Append('\n')
                            .Append("- Total Exception: ").Append(MiyakoCarryServicePlugin.LogBuffer.GetLogCount).Append("\n");

                        stringBuilder.Append("\n");
                        foreach (var logEntry in MiyakoCarryServicePlugin.LogBuffer.GetEntries().Values)
                        {
                            stringBuilder.Append(logEntry.Condition).Append($" ({logEntry.Total})").Append("\n");
                            stringBuilder.Append(logEntry.StackTrace).Append("\n");
                        }
                        stringBuilder.Append("```");

                        GUIUtility.systemCopyBuffer = stringBuilder.ToString();
                        NotificationManager.DisplayMessageNotification(string.Format(Locales.BIGSURVEYNOTIFY.McsLocalized(), MiyakoCarryServicePlugin.LogBuffer.GetLogCount), textColor: new Il2CppSystem.Nullable<Color>());
                        NewBigSurveyCount = 0;
                        _animatedToggle.ToggleSilent(false);
                    }));
                }
                _hoverTooltipArea = _bigSurveyGameObject.GetComponentInChildren<HoverTooltipArea>();
                SetBigSurveyButtonInteractable(true);

                MiyakoCarryServicePlugin.LogBuffer.SettlePendingCount();
                RefreshBigSurveyBadge();
            }
            ShowMcsBotPlayerInventoryModeInfo(false);
        }

        private static string BuildRaidUsedInfoSections()
        {
            var sectionsBuilder = new StringBuilder();
            sectionsBuilder.Append("- Raid Info:");

            var raidUsedInfos = MiyakoCarryServicePlugin.LogBuffer.GetRaidUsedInfos();
            if (raidUsedInfos.Count == 0)
            {
                sectionsBuilder.Append('\n').Append("  Non-Host");
                return sectionsBuilder.ToString();
            }

            foreach (var raidUsedInfo in raidUsedInfos)
            {
                sectionsBuilder.Append('\n').Append($"  [Raid#{raidUsedInfo.RaidIndex} {raidUsedInfo.LocationId}]:")
                    .Append('\n').Append("  - Used Brain: ").Append(FormatRaidUsedEntries(raidUsedInfo.UsedBrains))
                    .Append('\n').Append("  - Used Layer: ").Append(FormatRaidUsedEntries(raidUsedInfo.UsedLayers))
                    .Append('\n').Append("  - Used Reason: ").Append(FormatRaidUsedEntries(raidUsedInfo.UsedReasons));
            }

            return sectionsBuilder.ToString();
        }

        private static string FormatRaidUsedEntries(IEnumerable<KeyValuePair<string, int>> entries)
        {
            if (entries == null || !entries.Any())
            {
                return "Non-Host";
            }

            return string.Join(", ", entries.Select(kvp => $"{kvp.Key}({kvp.Value})"));
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

                matchingStatus.gameObject.SetActive(true);

                var matchingStatusFitter = matchingStatus.GetComponent<ContentSizeFitter>();
                if (matchingStatusFitter == null)
                {
                    matchingStatusFitter = matchingStatus.gameObject.AddComponent<ContentSizeFitter>();
                }
                matchingStatusFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

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
                    button.onClick.AddListener(new Action(() =>
                    {
                        GetContextInteractionsPatch.OnExitMcsBotPlayerInventoryMode(GetContextInteractionsPatch.McsBotPlayerAid).McsHandleExceptions();
                    }));
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
}
