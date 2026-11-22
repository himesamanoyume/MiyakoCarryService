using UnityEngine;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Utils;
using System.Text.RegularExpressions;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Events;
using SPTushonka.Reflection.Patching;
using EFT;
using EFT.Communications;
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using SPTushonka.Reflection.Il2Cpp;

namespace MiyakoCarryService.Client
{
    [BepInPlugin(McsGUID, McsPluginName, BepInExClientVersion)]
    [BepInProcess(EFTapp)]
    [BepInDependency(BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class MiyakoCarryServicePlugin : BasePlugin
    {
        public const string BepInExClientVersion = "1.99.1";
        public static System.Version ClientVersion { get; } = new(BepInExClientVersion);
        public const string EFTapp = "EscapeFromTarkov.exe";
        public const string McsGUID = "top.himesamanoyume.miyakocarryservice";
        public const string FikaGUID = "com.fika.core";
        public const string BigBrainGUID = "xyz.drakia.bigbrain";
#if DEBUG
        public const string McsPluginName = "姫様の夢 MiyakoCarryService DebugBuild";
#else
        public const string McsPluginName = "姫様の夢 MiyakoCarryService";
#endif
        public const string MiyakoTraderId = "6952ced4bcc1dd1e3c80dfcb";
        public static MiyakoCarryServicePlugin Instance;
        public static McsPluginClientConfig McsPluginClientConfig = null;
        private PatchManager _patchManager;
        private Regex _stackRegex = new(@"\s*\(at <[^>]+>:\d+\)", RegexOptions.Compiled);
        internal static LogBuffer LogBuffer = new LogBuffer();
        private Debouncer<string, McsBotPlayerConfig> _configDebouncer;

        #region BASIC

        public static ConfigEntry<bool> EnableLooting;
        public static ConfigEntry<KeyboardShortcut> EnableLootingHotKey;
        public static ConfigEntry<int> PriceThreshold;
        public static ConfigEntry<string> KeywordItemText;
        public static ConfigEntry<bool> LootingKeywordItem;
        public static ConfigEntry<EBlockItemType> BlockItemType;
        public static ConfigEntry<bool> EnableKeepFormation;
        public static ConfigEntry<KeyboardShortcut> EnableKeepFormationHotKey;
        public static ConfigEntry<string> FormationMatrix;
        public static ConfigEntry<float> FormationSpacing;
        public static ConfigEntry<bool> FormationSequentialFill;
        public static ConfigEntry<KeyboardShortcut> SaveFormationPresetHotKey;
        public static ConfigEntry<string> FormationPresets;
        public static ConfigEntry<bool> PhrasesSilent;
        public static ConfigEntry<bool> EnableSubtitles;

        #endregion

        #region COMMAND

        public static ConfigEntry<KeyboardShortcut> CommandHotKey;

        #endregion

        #region PLAYER

        public static ConfigEntry<bool> TeammateHighlight;
        public static ConfigEntry<KeyboardShortcut> TeammateHighlightHotKey;
        public static ConfigEntry<Color> TeammateHighlightColor;
        public static ConfigEntry<bool> ShowBrevityCode;

        #endregion

        #region DEBUG

        public static ConfigEntry<bool> DrawDebugInfo;
        public static ConfigEntry<int> DebugTextSize;

#if DEBUG
        public static ConfigEntry<bool> McsBotPlayerNoDamage;
        public static ConfigEntry<bool> McsBotPlayerKeepAlive;
        public static ConfigEntry<bool> EnableMcsLayer;
#endif

        #endregion

        public override void Load()
        {
            Instance = this;

            MainThread.Install(this);
            Il2CppExceptionSafety.Install();
            Application.add_logMessageReceived(DelegateSupport.ConvertDelegate<Application.LogCallback>(new Action<string, string, LogType>(OnLog)));
            RegisterAll();
            SetupConfig();
            DefaultLang = LocalizationManager.Instance.Culture;
            foreach (var kvp in LocalLocales.LoadingLocales)
            {
                LocalizationManager.Instance.UpdateLocales(kvp.Key, kvp.Value.ToIl2CppDictionary());
            }

            McsPluginClientConfig = McsRequestHandler.GetMcsPluginClientConfig();
            EnableAllPatches();
            GameLoop.Instance.Init();
        }

        public override bool Unload()
        {
            if (MiyakoCarryServiceSingleton<GameLoop>.Instantiated)
            {
                GameLoop.Instance.Destroy();
            }

            _patchManager?.DisablePatches();

            EventMgr.OnMgrDestroy();
            return true;
        }

        private static bool _isHandlingLog = false;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (_isHandlingLog)
            {
                return;
            }

            _isHandlingLog = true;

            try
            {
                if (LogBuffer == null || condition == null)
                {
                    return;
                }

                if (condition.Contains("GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced."))
                {
                    return;
                }

                if (type is LogType.Exception or LogType.Error)
                {
                    LogBuffer.AddEntryIfNotFull(condition, _stackRegex.Replace(stackTrace ?? string.Empty, ""));
                }
            }
            catch (System.Exception ex)
            {
                McsLogger.LogError($"[MiyakoCarryServicePlugin.OnLog] {ex}");
            }
            finally
            {
                _isHandlingLog = false;
            }
        }

        public bool CheckPlugin(List<string> pluginList)
        {
            var pluginInfos = new List<PluginInfo>(IL2CPPChainloader.Instance.Plugins.Values);

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
            _patchManager = new PatchManager(this, true);
            _patchManager.EnablePatches();
        }

        private static readonly Dictionary<string, ConfigSection> _sections = new();
        public static readonly List<string> HideList = new();

        internal ConfigEntry<T> Register<T>(
            string sectionName,
            int order,
            string key,
            T defaultValue,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            ConfigurationManagerAttributes customAttributes = null,
            bool needNotify = true,
            bool isHide = false
        )
        {
            if (!_sections.TryGetValue(sectionName, out var section))
            {
                section = new ConfigSection(sectionName, order);
                _sections[sectionName] = section;
            }

            var attributes = customAttributes ?? new ConfigurationManagerAttributes();

            if (isHide)
            {
                HideList.Add(key);
            }

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
                        NotificationManager.DisplayMessageNotification($"{configEntry.Definition.Key.McsLocalized()} <color=#00ff00>{Locales.FUNCTIONENABLED.McsLocalized()}</color>", textColor: new Il2CppSystem.Nullable<Color>());
                    }
                    else
                    {
                        NotificationManager.DisplayMessageNotification($"{configEntry.Definition.Key.McsLocalized()} <color=#ff0000>{Locales.FUNCTIONDISABLED.McsLocalized()}</color>", textColor: new Il2CppSystem.Nullable<Color>());
                    }
                };
            }

            if (sectionName == Locales.BASIC)
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
                        BlockItemType = (int)BlockItemType.Value,
                        FormationMatrix = FormationMatrix.Value,
                        EnableKeepFormation = EnableKeepFormation.Value,
                        FormationSpacing = FormationSpacing.Value,
                        FormationSequentialFill = FormationSequentialFill.Value,
                        PhrasesSilent = PhrasesSilent.Value,
                        EnableSubtitles = EnableSubtitles.Value,
                        Extensions = McsBotPlayerConfigUtils.Snapshot()
                    });
                };
            }

            return configEntry;
        }

        internal ConfigEntry<T> Register<T>(
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
            var configTypeText = type switch
            {
                EConfigType.BASIC => Locales.BASIC,
                EConfigType.COMMAND => Locales.COMMAND,
                EConfigType.PLAYER => Locales.PLAYER,
                EConfigType.EXPERIMENTAL => Locales.EXPERIMENTAL,
                EConfigType.DEBUG or _ => Locales.DEBUG
            };
            return Register(configTypeText, (int)type, key, defaultValue, description, acceptableValues, customAttributes, needNotify, isHide);
        }

        internal void CustomDrawer<T>(ConfigEntryBase entry, Dictionary<T, string> dict, int xCount) where T : Enum
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

            var newIndex = Tools.DrawSelectionGrid(selectedIndex, options, xCount);
            if (newIndex != selectedIndex)
            {
                entry.BoxedValue = values.GetValue(newIndex);
            }
        }

        public void RegisterAll()
        {
            if (!TomlTypeConverter.CanConvert(typeof(Color)))
            {
                TomlTypeConverter.AddConverter(typeof(Color), new TypeConverter
                {
                    ConvertToString = (value, _) => ColorToHex((Color)value),
                    ConvertToObject = (value, _) => HexToColor((string)value),
                });
            }

            if (!TomlTypeConverter.CanConvert(typeof(KeyboardShortcut)))
            {
                TomlTypeConverter.AddConverter(typeof(KeyboardShortcut), new TypeConverter
                {
                    ConvertToString = (value, _) => ((KeyboardShortcut)value).Serialize(),
                    ConvertToObject = (value, _) => KeyboardShortcut.Deserialize((string)value),
                });
            }
        }

        private string ColorToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGBA(color);   // "RRGGBBAA"
        }

        private Color HexToColor(string text)
        {
            if (text[0] != '#')
            {
                text = "#" + text;
            }
            return ColorUtility.TryParseHtmlString(text, out var color) ? color : Color.white;
        }

        private void SetupConfig()
        {
            #region BASIC

            EnableLooting = Register(
                EConfigType.BASIC,
                Locales.ENABLELOOTING_KEY,
                false,
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

            EnableKeepFormation = Register(
                EConfigType.BASIC,
                Locales.ENABLEKEEPFORMATION_KEY,
                false,
                Locales.ENABLEKEEPFORMATION_DESCRIPTION
            );

            EnableKeepFormationHotKey = Register(
                EConfigType.BASIC,
                Locales.ENABLEKEEPFORMATIONHOTKEY_KEY,
                new KeyboardShortcut()
            );

            FormationMatrix = Register(
                EConfigType.BASIC,
                Locales.FORMATIONMATRIX_KEY,
                Tools.ResetFormationMatrix(),
                Locales.FORMATIONMATRIX_DESCRIPTION,
                customAttributes: new ConfigurationManagerAttributes
                {
                    CustomDrawer = static entry =>
                    {
                        var oldFormationMatrix = (string)entry.BoxedValue;
                        var newFormationMatrix = Tools.DrawFormationMatrix(entry.Definition.Key, oldFormationMatrix);
                        if (newFormationMatrix != oldFormationMatrix)
                        {
                            entry.BoxedValue = newFormationMatrix;
                        }
                    }
                }
            );

            FormationSpacing = Register(
                EConfigType.BASIC,
                Locales.FORMATIONSPACING_KEY,
                3f,
                acceptableValues: new AcceptableValueRange<float>(2f, 6f)
            );

            FormationSequentialFill = Register(
                EConfigType.BASIC,
                Locales.FORMATIONSEQUENTIALFILL_KEY,
                false,
                Locales.FORMATIONSEQUENTIALFILL_DESCRIPTION
            );

            SaveFormationPresetHotKey = Register(
                EConfigType.BASIC,
                Locales.SAVEFORMATIONPRESETHOTKEY_KEY,
                new KeyboardShortcut()
            );

            PhrasesSilent = Register(
                EConfigType.BASIC,
                Locales.PHRASESSILENT_KEY,
                false,
                Locales.PHRASESSILENT_DESCRIPTION
            );

            EnableSubtitles = Register(
                EConfigType.BASIC,
                Locales.ENABLESUBTITLES_KEY,
                true
            );

            FormationPresets = Register(
                EConfigType.BASIC,
                "FormationPresets",
                "[]",
                isHide: true
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

            ShowBrevityCode = Register(
                EConfigType.PLAYER,
                Locales.SHOWBREVITYCODE_KEY,
                true,
                Locales.SHOWBREVITYCODE_DESCRIPTION
            );

            #endregion
            #region DEBUG

            DrawDebugInfo = Register(
                EConfigType.DEBUG,
                Locales.DRAWDEBUGINFO_KEY,
                false
            );

            DebugTextSize = Register(
                EConfigType.DEBUG,
                Locales.DEBUGINFOTEXTSIZE_KEY,
                10,
                acceptableValues: new AcceptableValueRange<int>(5, 50)
            );

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
                    GameLoop.Instance,
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
                    McsLogger.LogError($"Batch refresh mcsBotPlayerConfig error: {e}");
                }
            }
        }
    }
}