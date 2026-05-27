using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using System;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Client.Patches.RefreshQuests;
using MiyakoCarryService.Client.Patches.Raid;
using MiyakoCarryService.Client.Patches.Bots;
using MiyakoCarryService.Client.Patches.Group;
using MiyakoCarryService.Client.Patches.BepInEx;
using MiyakoCarryService.Client.Patches.Events;
using System.Text.RegularExpressions;
using MiyakoCarryService.Client.Patches.BigSurvey;
using MiyakoCarryService.Client.Extensions;
using BepInEx.Bootstrap;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Patches.Inventory;

namespace MiyakoCarryService.Client
{
    [BepInPlugin(McsGUID, McsPluginName, BepInExClientVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    [BepInDependency(BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(FikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(McsFikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MiyakoCarryServicePlugin : BaseUnityPlugin
    {
        public const string BepInExClientVersion = "0.3.6.0";
        public static Version ClientVersion { get; } = new(BepInExClientVersion);
        public const string McsGUID = "top.himesamanoyume.miyakocarryservice";
        public const string FikaGUID = "com.fika.core";
        public const string McsFikaGUID = "top.himesamanoyume.miyakocarryservice.fika";
        public const string BigBrainGUID = "xyz.drakia.bigbrain";
        public const string McsPluginName = "姫様の夢 MiyakoCarryService";
        public static MiyakoCarryServicePlugin Instance;
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MiyakoCarryService");
        public static bool FikaInstalled { get; private set; } = false;
        public static bool IsFikaHeadless { get; private set; } = false;
        public static bool SAINInstalled { get; private set; } = false;
        private Regex _stackRegex = new(@"\s*\(at <[^>]+>:\d+\)", RegexOptions.Compiled);
        public static LogBuffer LogBuffer = new LogBuffer();
        private Debouncer<string, McsBotPlayerConfig> _configDebouncer;

        #region BASIC

        public static ConfigEntry<bool> EnableLooting;
        public static ConfigEntry<int> PriceThreshold;
        public static ConfigEntry<string> KeywordItemText;
        public static ConfigEntry<bool> LootingKeywordItem;
        public static ConfigEntry<EBlockItemType> BlockItemType;

        #endregion

        #region COMMAND

        public static ConfigEntry<KeyboardShortcut> CommandHotKey;

        #endregion

        #region PLAYER

        public static ConfigEntry<bool> TeammateHighlight;
        public static ConfigEntry<KeyboardShortcut> TeammateHighlightHotKey;
        public static ConfigEntry<Color> TeammateHighlightColor;
        public static ConfigEntry<bool> EnableSubtitles;

        #endregion

        #region DEBUG

#if DEBUG
        public static ConfigEntry<bool> McsBotPlayerNoDamage;
#endif

        #endregion

        void Awake()
        {
            Instance = this;
            new ContainsSearchStringPatch().Enable();
            new DrawSinglePluginPatch().Enable();
            new DrawFlagsFieldPatch().Enable();
        }

        void Start()
        {
            Application.logMessageReceived += OnLog;
            FikaInstalled = !CheckPlugin([FikaGUID]);
            IsFikaHeadless = !CheckPlugin(["com.fika.headless"]);
            SAINInstalled = !CheckPlugin(["me.sol.sain"]);
            SetupConfig();
            DefaultLang = LocaleManagerClass.LocaleManagerClass.String_0;
            foreach (var kvp in LocalLocales.LoadingLocales)
            {
                LocaleManagerClass.LocaleManagerClass.UpdateLocales(kvp.Key, kvp.Value);
            }
            EnableAllPatches();
            GameLoop.Instance.Init();
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (LogBuffer == null)
            {
                return;
            }

            if (type == LogType.Exception || type == LogType.Error)
            {
                LogBuffer.AddEntryIfNotFull(condition, _stackRegex.Replace(stackTrace, ""));
            }
        }

        public bool CheckPlugin(List<string> pluginList)
        {
            var pluginInfos = new List<PluginInfo>(Chainloader.PluginInfos.Values);

            foreach (PluginInfo Info in pluginInfos)
            {
                if (pluginList.Contains(Info.Metadata.GUID))
                {
                    return false;
                }
            }
            return true;
        }

        public static string DefaultLang = "en";

        public bool CheckUnsupportedPlugin()
        {
            return CheckPlugin([]);
        }

        private void EnableAllPatches()
        {
            new TraderScreensGroupShowPatch().Enable();
            new RaidSettingsLocalPatch().Enable();
            new MainMenuControllerClassPatch().Enable();
            new MatchMakerAcceptScreenReadyPatch().Enable();
            new TryLoadBotsProfilesOnStartPatch().Enable();
            new AddEnemyPatch().Enable();
            new TraderClassConstructorPatch().Enable();
            new TraderControllerClassConstructorPatch().Enable();
            new TraderControllerClassAddItemEventInvokePatch().Enable();
            new TraderControllerClassRemoveItemEventInvokePatch().Enable();
            new TraderControllerClassOutProcessPatch().Enable();
            new TraderControllerClassInProcessPatch().Enable();
            new ApplyDamagePatch().Enable();
            new OnGameStartedPatch().Enable();
            new RaidEndedPatch().Enable();
            new BotHearingSensorPatch().Enable();
            new PlayerSayPatch().Enable();
            new PlayHitEffectPatch().Enable();
            new TransitPointPatch1().Enable();
            new TransitPointPatch2().Enable();
            new MatchmakerTimeHasComePatch().Enable();
            new MatchMakerAcceptScreenCallbackPatch().Enable();
            new GroupPlayerViewModelClassPatch().Enable();
            new GetDailyQuestsPatch().Enable();
            new MatchmakerAcceptScreenShowPatch().Enable();
            new MatchingAbortPatch().Enable();
            new DisbandRaidGroupPatch().Enable();
            new MenuTaskBarAwakePatch().Enable();
            new NewNewsCountPatch().Enable();
            new SetGoalEnemyPatch().Enable();
            // new InitRepeatableQuestsDisposePatch().Enable();
            new ChatSendMessagePatch().Enable();
            new LocalQuestControllerClassPatch().Enable();
            new RaidReadyListFixAidPatch().Enable();
            new GetContextInteractionsPatch().Enable();
            new ContextInteractionsClassPatch().Enable();
            new GetProfilesPatch().Enable();
            new MenuScreenPatch().Enable();
            new TryFindChangedContainerPatch().Enable();
            new CanModifyItemPatch().Enable();
            new ItemSubtract1Patch().Enable();
            new ItemSubtract2Patch().Enable();
            new TryReloadPatch().Enable();
            new BotWeaponSelectorPatch().Enable();
            new AdvAssaultTargetPatch().Enable();
            new InitVaultComponentPatch().Enable();

            if (FikaInstalled)
            {

            }

#if DEBUG

#endif
        }

        private static readonly Dictionary<EConfigType, ConfigSection> _sections = new();
        public static readonly List<string> CheaterEditionOnlyList = new();
        public static readonly List<string> HideList = new();

        private class ConfigSection
        {
            private int _currentOrder;

            public string Name { get; }

            public ConfigSection(EConfigType type)
            {
                Name = GetSection(type);
                _currentOrder = GetOrder(type);
            }

            public int GetNextOrder() => _currentOrder--;
        }

        private ConfigEntry<T> Register<T>(
            EConfigType type,
            string key,
            T defaultValue,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            ConfigurationManagerAttributes customAttributes = null,
            bool needNotify = true,
            bool isCheaterEditionOnly = false,
            bool isHide = false
        )
        {
            if (!_sections.TryGetValue(type, out var section))
            {
                section = new ConfigSection(type);
                _sections[type] = section;
            }

            if (isCheaterEditionOnly)
            {
                CheaterEditionOnlyList.Add(key);
            }

            if (isHide)
            {
                HideList.Add(key);
            }

            var attributes = customAttributes ?? new ConfigurationManagerAttributes();
            attributes.Order = section.GetNextOrder();

            var configDescription = new ConfigDescription(
                description,
                acceptableValues,
                attributes
            );

            var configEntry = Config.Bind(
                section.Name,
                key,
                defaultValue,
                configDescription
            );

            if (typeof(T) == typeof(bool) && needNotify)
            {
                configEntry.SettingChanged += (object sender, EventArgs e) =>
                {
                    var entry = (ConfigEntryBase)sender;
                    if ((bool)entry.BoxedValue)
                    {
                        NotificationManagerClass.DisplayMessageNotification($"{configEntry.Definition.Key.McsLocalized()} <color=#00ff00>{Locales.FUNCTIONENABLED.McsLocalized()}</color>");
                    }
                    else
                    {
                        NotificationManagerClass.DisplayMessageNotification($"{configEntry.Definition.Key.McsLocalized()} <color=#ff0000>{Locales.FUNCTIONDISABLED.McsLocalized()}</color>");
                    }
                };
            }

            if (type == EConfigType.BASIC)
            {
                configEntry.SettingChanged += (object sender, EventArgs e) =>
                {
                    if (!GameLoop.Instance.IsVaildGameWorld)
                    {
                        return;
                    }

                    DebouncedConfigSync(new McsBotPlayerConfig
                    {
                        McsLeadPlayerId = GameLoop.Instance.Session.Profile.Id,
                        EnableLooting = EnableLooting.Value,
                        PriceThreshold = PriceThreshold.Value,
                        KeywordItemText = KeywordItemText.Value,
                        LootingKeywordItem = LootingKeywordItem.Value,
                        BlockItemType = (int)BlockItemType.Value
                    });
                };
            }

            return configEntry;
        }

        private static void CustomDrawer<T>(ConfigEntryBase entry, Dictionary<T, string> dict, int xCount) where T : Enum
        {
            var value = (T)entry.BoxedValue;
            var values = Enum.GetValues(typeof(T));
            var options = new string[values.Length];
            var selectedIndex = 0;

            for (int i = 0; i < values.Length; i++)
            {
                var enumValue = (T)values.GetValue(i);
                options[i] = dict.ContainsKey(enumValue) ? dict[enumValue] : enumValue.ToString();
                if (enumValue.Equals(value))
                {
                    selectedIndex = i;
                }
            }

            var newIndex = GUILayout.SelectionGrid(selectedIndex, options, xCount);
            if (newIndex != selectedIndex)
            {
                entry.BoxedValue = values.GetValue(newIndex);
            }
        }

        private static string GetSection(EConfigType configType)
        {
            return configType switch
            {
                EConfigType.BASIC => Locales.BASIC,
                EConfigType.COMMAND => Locales.COMMAND,
                EConfigType.PLAYER => Locales.PLAYER,
                EConfigType.DEBUG or _ => Locales.DEBUG
            };
        }

        private static int GetOrder(EConfigType configType)
        {
            return configType switch
            {
                EConfigType.BASIC => 100,
                EConfigType.COMMAND => 200,
                EConfigType.PLAYER => 300,
                EConfigType.DEBUG or _ => 2000
            };
        }

        private void SetupConfig()
        {
            #region BASIC

            EnableLooting = Register(
                EConfigType.BASIC,
                Locales.ENABLELOOTING_KEY,
                true,
                Locales.ENABLELOOTING_DESCRIPTION
            );

            PriceThreshold = Register(
                EConfigType.BASIC,
                Locales.PRICETHRESHOLD_KEY,
                10000,
                Locales.PRICETHRESHOLD_DESCRIPTION,
                new AcceptableValueRange<int>(0, 1500000)
            );

            KeywordItemText = Register(
                EConfigType.BASIC,
                Locales.KEYWORDITEMTEXT_KEY,
                "",
                Locales.KEYWORDITEMTEXT_DESCRIPTION
            );

            LootingKeywordItem = Register(
                EConfigType.BASIC,
                Locales.LOOTINGKEYWORDITEM_KEY,
                true
            );

            BlockItemType = Register(
                EConfigType.BASIC,
                Locales.BLOCKITEMTYPE_KEY,
                (EBlockItemType)0
            );

            #endregion
            #region COMMAND

            CommandHotKey = Register(
                EConfigType.COMMAND,
                Locales.COMMANDHOTKEY_KEY,
                new KeyboardShortcut()
            );

            #endregion
            #region PLAYER

            TeammateHighlight = Register(
                EConfigType.PLAYER,
                Locales.TEAMMATEHIGHLIGHT_KEY,
                false
            );

            TeammateHighlightHotKey = Register(
                EConfigType.PLAYER,
                Locales.TEAMMATEHIGHLIGHTHOTKEY_KEY,
                new KeyboardShortcut()
            );

            TeammateHighlightColor = Register(
                EConfigType.PLAYER,
                Locales.TEAMMATEHIGHLIGHTCOLOR_KEY,
                Draw.TranslucentTianyi.Rgb
            );

            EnableSubtitles = Register(
                EConfigType.PLAYER,
                Locales.ENABLESUBTITLES_KEY,
                true
            );

            #endregion
            #region DEBUG

#if DEBUG
            McsBotPlayerNoDamage = Register(
                EConfigType.DEBUG,
                "护航无敌",
                true,
                customAttributes: new ConfigurationManagerAttributes() { IsAdvanced = true }
            );
#endif

            #endregion
        }

        public void DebouncedConfigSync(McsBotPlayerConfig mcsBotPlayerConfig)
        {
            if (_configDebouncer == null)
            {
                _configDebouncer = new Debouncer<string, McsBotPlayerConfig>(
                    this,
                    1f,
                    ExecuteConfigChanges
                );
            }

            if (_configDebouncer != null)
            {
                _configDebouncer.Trigger(McsGUID, mcsBotPlayerConfig);
            }
        }

        private void ExecuteConfigChanges(Dictionary<string, McsBotPlayerConfig> configs)
        {
            foreach (var kvp in configs)
            {
                try
                {
                    EventMgr.Notify(new ConfigEntrySettingChangedEvent
                    {
                        McsBotPlayerConfig = kvp.Value
                    });
                }
                catch (Exception e)
                {
                    Logger.LogError($"Batch refresh mcsBotPlayerConfig error: {e}");
                }
            }
        }
    }
}

