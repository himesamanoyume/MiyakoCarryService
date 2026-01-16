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

namespace MiyakoCarryService.Client;

[BepInPlugin(MiyakoCarryServiceGUID, MiyakoCarryServicePluginName, BepInExClientVersion)]
[BepInProcess("EscapeFromTarkov.exe")]
public sealed class MiyakoCarryServicePlugin : BaseUnityPlugin
{
    public const string BepInExClientVersion = "0.0.6.0";
    public static Version ClientVersion { get; } = new(BepInExClientVersion);
    public const string MiyakoCarryServiceGUID = "top.himesamanoyume.miyakocarryservice";
    public const string MiyakoCarryServicePluginName = "Himesamanoyume.MiyakoCarryService";
    public static MiyakoCarryServicePlugin Instance;
    public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MiyakoCarryService");
    public static bool FikaInstalled = false;
    public static bool IsFikaHeadless = false;

    #region BASIC

    #endregion

    #region DEBUG

#if DEBUG

#endif

    #endregion

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CheckFikaPlugin();
        CheckFikaHeadlessPlugin();
        SetupConfig();
        DefaultLang = LocaleManagerClass.LocaleManagerClass.String_0;
        EnableAllPatches();
        GameLoop.Instance.Init();
    }

    public static string DefaultLang = "en";

    public static bool CheckUnsupportedPlugin()
    {
        var unsupportedPluginList = new List<string>()
        {

        };
        return CheckPlugin(unsupportedPluginList);
    }

    public static bool CheckFikaHeadlessPlugin()
    {
        var unsupportedPluginList = new List<string>()
        {
            "com.fika.headless"
        };
        IsFikaHeadless = !CheckPlugin(unsupportedPluginList);
        return IsFikaHeadless;
    }

    public static bool CheckFikaPlugin()
    {
        var fikaPlugin = new List<string>()
        {
            "com.fika.core"
        };

        FikaInstalled = !CheckPlugin(fikaPlugin);
        return FikaInstalled;
    }

    private static bool CheckPlugin(List<string> pluginList)
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

    private void EnableAllPatches()
    {
        if (FikaInstalled)
        {

        }
#if CHEATERCARRY

#endif
        if (IsFikaHeadless)
        {
            return;
        }
        new TraderScreensGroupShowPatch().Enable();
        new RaidSettingsLocalPatch().Enable();
        new MatchMakerAcceptScreenPatch().Enable();
        new TryLoadBotsProfilesOnStartPatch().Enable();
        new AddEnemyPatch().Enable();
        new ManualUpdatePatch().Enable();
#if DEBUG
        
#endif
    }

    private static readonly Dictionary<EConfigType, ConfigSection> _sections = new();

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
        ConfigurationManagerAttributes customAttributes = null
        )
    {
        if (!_sections.TryGetValue(type, out var section))
        {
            section = new ConfigSection(type);
            _sections[type] = section;
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
        
    }
}
