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
using MiyakoCarryService.Client.Patches.SAIN;
using System.IO;
using System.Reflection;
using SPT.Reflection.Patching;
using MiyakoCarryService.Client.Patches.Interactive;

namespace MiyakoCarryService.Client
{
    [BepInPlugin(McsGUID, McsPluginName, BepInExClientVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    [BepInDependency(BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(FikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(McsFikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MiyakoCarryServicePlugin : BaseUnityPlugin
    {
        public const string BepInExClientVersion = "1.0.9.0";
        public static Version ClientVersion { get; } = new(BepInExClientVersion);
        public const string McsGUID = "top.himesamanoyume.miyakocarryservice";
        public const string FikaGUID = "com.fika.core";
        public const string McsFikaGUID = "top.himesamanoyume.miyakocarryservice.fika";
        public const string BigBrainGUID = "xyz.drakia.bigbrain";
#if DEBUG
        public const string McsPluginName = "姫様の夢 MiyakoCarryService DebugBuild";
#else
        public const string McsPluginName = "姫様の夢 MiyakoCarryService";
#endif
        public const string MiyakoTraderId = "6952ced4bcc1dd1e3c80dfcb";
        public static MiyakoCarryServicePlugin Instance;
        public static McsPluginClientConfig McsPluginClientConfig = null;
        private List<ModulePatch> _patches = new();
        private object _mcsFika = null;
        private Type _mcsFikaType = null;
        public static bool IsLoadedByScriptEngine = false;
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MiyakoCarryService");
        public static bool FikaInstalled { get; private set; } = false;
        public static bool IsFikaHeadless { get; private set; } = false;
        public static bool SAINInstalled { get; private set; } = false;
        private Regex _stackRegex = new(@"\s*\(at <[^>]+>:\d+\)", RegexOptions.Compiled);
        public static LogBuffer LogBuffer = new LogBuffer();
        private Debouncer<string, McsBotPlayerConfig> _configDebouncer;

        #region BASIC

        public static ConfigEntry<bool> EnableLooting;
        public static ConfigEntry<KeyboardShortcut> EnableLootingHotKey;
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
        public static ConfigEntry<bool> McsBotPlayerKeepAlive;
        public static ConfigEntry<bool> EnableMcsLayer;
#endif

        #endregion

        void Awake()
        {
            Instance = this;
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                IsLoadedByScriptEngine = true;
            }
        }

        void Start()
        {
            Application.logMessageReceived += OnLog;
            FikaInstalled = !CheckPlugin([FikaGUID]);
            IsFikaHeadless = !CheckPlugin(["com.fika.headless"]);
            SAINInstalled = !CheckPlugin(["me.sol.sain"]);
            SetupConfig();
            McsPluginClientConfig = McsRequestHandler.GetMcsPluginClientConfig();
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

            if (condition.Contains("GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced."))
            {
                return;
            }

            if (type is LogType.Exception or LogType.Error)
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
            _patches.Add(new ContainsSearchStringPatch());
            _patches.Add(new DrawSinglePluginPatch());
            _patches.Add(new DrawFlagsFieldPatch());
            _patches.Add(new TraderScreensGroupShowPatch());
            _patches.Add(new RaidSettingsLocalPatch());
            _patches.Add(new MainMenuControllerClassPatch());
            _patches.Add(new MatchMakerAcceptScreenReadyPatch());
            _patches.Add(new BotsControllerInitPatch());
            _patches.Add(new AddEnemyPatch());
            _patches.Add(new TraderClassConstructorPatch());
            _patches.Add(new TraderControllerClassConstructorPatch());
            _patches.Add(new TraderControllerClassAddItemEventInvokePatch());
            _patches.Add(new TraderControllerClassRemoveItemEventInvokePatch());
            _patches.Add(new TraderControllerClassOutProcessPatch());
            _patches.Add(new TraderControllerClassInProcessPatch());
            _patches.Add(new ApplyDamagePatch());
            _patches.Add(new OnGameStartedPatch());
            _patches.Add(new RaidEndedPatch());
            // _patches.Add(new BotHearingSensorPatch());
            // _patches.Add(new PlayerSayPatch());
            // _patches.Add(new PlayHitEffectPatch());
            _patches.Add(new TransitPointPatch1());
            _patches.Add(new TransitPointPatch2());
            _patches.Add(new MatchmakerTimeHasComePatch());
            _patches.Add(new MatchMakerAcceptScreenCallbackPatch());
            _patches.Add(new GroupPlayerViewModelClassPatch());
            _patches.Add(new GetDailyQuestsPatch());
            _patches.Add(new MatchmakerAcceptScreenShowPatch());
            _patches.Add(new MatchingAbortPatch());
            _patches.Add(new DisbandRaidGroupPatch());
            _patches.Add(new MenuTaskBarAwakePatch());
            _patches.Add(new NewNewsCountPatch());
            _patches.Add(new SetGoalEnemyPatch());
            _patches.Add(new ChatSendMessagePatch());
            _patches.Add(new LocalQuestControllerClassPatch());
            _patches.Add(new RaidReadyListFixAidPatch());
            _patches.Add(new GetContextInteractionsPatch());
            _patches.Add(new ContextInteractionsClassPatch());
            _patches.Add(new GetProfilesPatch());
            _patches.Add(new MenuScreenPatch());
            _patches.Add(new TryFindChangedContainerPatch());
            _patches.Add(new CanModifyItemPatch());
            _patches.Add(new ItemSubtract1Patch());
            _patches.Add(new ItemSubtract2Patch());
            _patches.Add(new TryReloadPatch());
            _patches.Add(new BotWeaponSelectorPatch());
            _patches.Add(new AdvAssaultTargetPatch());
            _patches.Add(new InitVaultComponentPatch());
            _patches.Add(new MatchMakerSideSelectionScreenPatch());
            _patches.Add(new ActionPanelAnchorPatch());
            _patches.Add(new PartyInfoPanelScrollPatch());
            _patches.Add(new EquipmentBuildsScreenShowPatch());
            _patches.Add(new BotFirstAidClassMinPercentPatch());
            _patches.Add(new OnBeenKilledByAggressorPatch());
            _patches.Add(new TriggerWithIdEnterPatch());
            _patches.Add(new TriggerWithIdExitPatch());
            _patches.Add(new GetPartToShootPatch());
            _patches.Add(new IsAllowedPlayerPatch());
            _patches.Add(new DeactivateMinePatch());
            _patches.Add(new DoorGetActionsClassPatch());
            _patches.Add(new LootItemGetActionsClassPatch());
            _patches.Add(new RefreshMedsPatch());

            if (FikaInstalled)
            {
                if (IsLoadedByScriptEngine)
                {
                    _patches.Add(new PlayerOnDeadPatch());
                }
                else
                {
                    LoadMcsFika();
                }
            }
            else
            {
                _patches.Add(new PlayerOnDeadPatch());
            }

            if (SAINInstalled)
            {
                _patches.Add(new CombatSoloLayerStartPatch());
                _patches.Add(new CombatSoloLayerIsActivePatch());
            }

#if DEBUG

#endif

            foreach (var patch in _patches)
            {
                patch.Enable();
            }
        }

        private void OnDestroy()
        {
            foreach (var patch in _patches)
            {
                patch.Disable();
            }
            UnloadMcsFika();
            GameLoop.Instance.Destroy();
            Destroy(this);
        }

        private void LoadMcsFika()
        {
            var pluginDir = IsLoadedByScriptEngine ? Path.Combine(BepInEx.Paths.PluginPath, "MiyakoCarryServiceClient") : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyPath = Path.Combine(pluginDir, "Himesamanoyume.MiyakoCarryServiceFika.dll");
            if (!File.Exists(assemblyPath))
            {
                return;
            }

            var assembly = Assembly.LoadFrom(assemblyPath);
            _mcsFikaType = assembly.GetType("MiyakoCarryService.Fika.MiyakoCarryServiceFika");

            if (_mcsFikaType != null)
            {
                _mcsFika = Activator.CreateInstance(_mcsFikaType);
                var initMethod = _mcsFikaType.GetMethod("InitMcsFika");
                initMethod?.Invoke(_mcsFika, null);
            }
        }

        // 我无法使用AppDomain和AssemblyLoadContext，因此只能不完全卸载
        private void UnloadMcsFika()
        {
            if (_mcsFikaType != null)
            {
                var cleanMethod = _mcsFikaType.GetMethod("CleanMcsFika");
                cleanMethod?.Invoke(_mcsFika, null);
            }
        }

        private static readonly Dictionary<EConfigType, ConfigSection> _sections = new();
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
            bool isHide = false
        )
        {
            if (!_sections.TryGetValue(type, out var section))
            {
                section = new ConfigSection(type);
                _sections[type] = section;
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

            EnableLootingHotKey = Register(
                EConfigType.BASIC,
                Locales.ENABLELOOTINGHOTKEY_KEY,
                new KeyboardShortcut()
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
                "护航免伤",
                true,
                customAttributes: new ConfigurationManagerAttributes() { IsAdvanced = true }
            );

            McsBotPlayerKeepAlive = Register(
                EConfigType.DEBUG,
                "护航锁血",
                true,
                customAttributes: new ConfigurationManagerAttributes() { IsAdvanced = true }
            );

            EnableMcsLayer = Register(
                EConfigType.DEBUG,
                "启用Mcs层级",
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

