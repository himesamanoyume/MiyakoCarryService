using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using System;
using BepInEx.Bootstrap;
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

namespace MiyakoCarryService.Client;

[BepInPlugin(McsGUID, McsPluginName, BepInExClientVersion)]
[BepInProcess("EscapeFromTarkov.exe")]
[BepInDependency(BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(FikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(McsFikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class MiyakoCarryServicePlugin : BaseUnityPlugin
{
    public const string BepInExClientVersion = "0.1.6.2";
    public static Version ClientVersion { get; } = new(BepInExClientVersion);
    public const string McsGUID = "top.himesamanoyume.miyakocarryservice";
    public const string FikaGUID = "com.fika.core";
    public const string McsFikaGUID = "top.himesamanoyume.miyakocarryservice.fika";
    public const string BigBrainGUID = "xyz.drakia.bigbrain";
    public const string McsPluginName = "姫様の夢 MiyakoCarryService";
    public static MiyakoCarryServicePlugin Instance;
    public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MiyakoCarryService");
    public static bool FikaInstalled { get; private set; }  = false;
    public static bool IsFikaHeadless { get; private set; } = false;
    private Regex _stackRegex = new(@"\s*\(at <[^>]+>:\d+\)", RegexOptions.Compiled);
    public static LogBuffer LogBuffer = new LogBuffer();

    #region BASIC

    public static ConfigEntry<int> PriceThreshold;
    public static ConfigEntry<int> ArmorLevelThreshold;
    public static ConfigEntry<bool> LootingWishlishItem;
    public static ConfigEntry<bool> LootingQuestItem;
    public static ConfigEntry<EBlockItemType> BlockItemType;

    #endregion

    #region DEBUG

#if DEBUG

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
        CheckFikaPlugin();
        CheckFikaHeadlessPlugin();
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

    public static string DefaultLang = "en";

    public static bool CheckUnsupportedPlugin()
    {
        var unsupportedPluginList = new List<string>()
        {

        };
        return Tools.CheckPlugin(unsupportedPluginList);
    }

    public static bool CheckFikaHeadlessPlugin()
    {
        IsFikaHeadless = !Tools.CheckPlugin(["com.fika.headless"]);
        return IsFikaHeadless;
    }

    public static bool CheckFikaPlugin()
    {
        FikaInstalled = !Tools.CheckPlugin([FikaGUID]);
        return FikaInstalled;
    }

    

    private void EnableAllPatches()
    {
        if (FikaInstalled)
        {

        }
#if CHEATERCARRY

#endif
        new TraderScreensGroupShowPatch().Enable();
        new RaidSettingsLocalPatch().Enable();
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
        new TransitPointPatch().Enable();
        new MatchmakerTimeHasComePatch().Enable();
        new MatchMakerAcceptScreenCallbackPatch().Enable();
        new GroupPlayerViewModelClassPatch().Enable();
        new GetDailyQuestsPatch().Enable();
        new MatchmakerAcceptScreenShowPatch().Enable();
        new MatchingAbortPatch().Enable();
        new DisbandRaidGroupPatch().Enable();
        // new ManualUpdatePatch().Enable();
        new MenuTaskBarAwakePatch().Enable();
        new NewNewsCountPatch().Enable();

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
            EConfigType.PLAYER => Locales.PLAYER,
            EConfigType.DEBUG or _ => Locales.DEBUG
        };
    }

    private static int GetOrder(EConfigType configType)
    {
        return configType switch
        {
            EConfigType.BASIC => 100,
            EConfigType.PLAYER => 200,
            EConfigType.DEBUG or _ => 2000
        };
    }

    private void SetupConfig()
    {
        PriceThreshold = Register(
            EConfigType.BASIC,
            Locales.PRICETHRESHOLD_KEY,
            10000,
            Locales.PRICETHRESHOLD_DESCRIPTION,
            new AcceptableValueRange<int>(0, 1500000)
        ); 

        ArmorLevelThreshold = Register(
            EConfigType.BASIC,
            Locales.ARMORLEVELTHRESHOLD_KEY,
            5,
            acceptableValues: new AcceptableValueRange<int>(1, 6)
        ); 

        LootingWishlishItem = Register(
            EConfigType.BASIC,
            Locales.LOOTINGWISHLISHITEM_KEY,
            true
        ); 

        LootingQuestItem = Register(
            EConfigType.BASIC,
            Locales.LOOTINGQUESTITEM_KEY,
            true
        ); 

        BlockItemType = Register(
            EConfigType.BASIC,
            Locales.BLOCKITEMTYPE_KEY,
            (EBlockItemType)0
        );
    }
}
