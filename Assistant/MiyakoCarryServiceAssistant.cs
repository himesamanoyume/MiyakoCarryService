using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Mgrs;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant
{
    [BepInPlugin(AssistantGUID, AssistantPluginName, MiyakoCarryServicePlugin.BepInExClientVersion)]
    [BepInProcess(MiyakoCarryServicePlugin.EFTapp)]
    [BepInDependency(MiyakoCarryServicePlugin.McsGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MiyakoCarryServicePlugin.FikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MiyakoCarryServiceAssistantPlugin : BaseUnityPlugin
    {
        public const string AssistantGUID = "top.himesamanoyume.miyakocarryservice.assistant";
#if DEBUG
        public const string AssistantPluginName = "姫様の夢 MiyakoCarryServiceAssistant DebugBuild";
#else
        public const string AssistantPluginName = "姫様の夢 MiyakoCarryServiceAssistant";
#endif

        public static MiyakoCarryServiceAssistantPlugin Instance;
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MiyakoCarryServiceAssistant");

        public static bool IsLoadedByScriptEngine = false;

        internal AssistantVoiceConfig VoiceConfig;

        #region Assistant

        public static ConfigEntry<bool> VoiceEnabled;
        public static ConfigEntry<EVoiceTriggerMode> VoiceTriggerMode;
        public static ConfigEntry<KeyboardShortcut> VoiceHotKey;
        public static ConfigEntry<float> VoiceCaptureMaxSeconds;
        public static ConfigEntry<float> VoiceVadEnergyThreshold;
        public static ConfigEntry<float> VoiceVadSilenceSeconds;
        public static ConfigEntry<bool> VoiceFeedbackSubtitles;
        public static ConfigEntry<ESttProvider> SttProvider;
        public static ConfigEntry<string> SttApiKey;
        public static ConfigEntry<string> SttBaseUrl;
        public static ConfigEntry<string> SttModel;
        public static ConfigEntry<string> SttLanguage;
        public static ConfigEntry<int> SttTimeoutSec;
        public static ConfigEntry<ELlmProvider> LlmProvider;
        public static ConfigEntry<string> LlmApiKey;
        public static ConfigEntry<string> LlmBaseUrl;
        public static ConfigEntry<string> LlmModel;
        public static ConfigEntry<string> LlmSystemPrompt;
        public static ConfigEntry<double> LlmTemperature;
        public static ConfigEntry<int> LlmMaxTokens;
        public static ConfigEntry<int> LlmTimeoutSec;

        #endregion

        void Awake()
        {
            Instance = this;
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                IsLoadedByScriptEngine = true;
            }
        }

        void Start()
        {
            SetupConfig();
            VoiceConfig = AssistantVoiceConfig.FromConfig();
            AssistantHttpClient.Initialize();

            McsEventApi.Notify(new GameLoopMgrEnableEvent
            {
                MgrTypes = [typeof(VoiceMgr)]
            });
        }

        private void SetupConfig()
        {
            const string section = Locales.ASSISTANT_SECTION;
            const int order = 400;

            VoiceEnabled = McsConfigApi.RegisterConfig(
                section, order,
                Locales.VOICEENABLED_KEY,
                false,
                Locales.VOICEENABLED_DESCRIPTION);

            VoiceTriggerMode = McsConfigApi.RegisterConfig(
                section, order,
                Locales.VOICETRIGGERMODE_KEY,
                EVoiceTriggerMode.PushToTalk,
                Locales.VOICETRIGGERMODE_DESCRIPTION,
                customAttributes: new ConfigurationManagerAttributes
                {
                    CustomDrawer = static entry => McsConfigApi.CustomDrawer(entry,
                        new Dictionary<EVoiceTriggerMode, string>
                        {
                            { EVoiceTriggerMode.PushToTalk, Locales.VOICETRIGGERMODEPUSH2TALK.McsLocalized() },
                            { EVoiceTriggerMode.FreeTalk, Locales.VOICETRIGGERMODEFREETALK.McsLocalized() },
                        }, 2
                    )
                });

            VoiceHotKey = McsConfigApi.RegisterConfig(
                section, order,
                Locales.VOICEHOTKEY_KEY,
                new KeyboardShortcut(),
                Locales.VOICEHOTKEY_DESCRIPTION);

            VoiceCaptureMaxSeconds = McsConfigApi.RegisterConfig(
                section, order,
                Locales.VOICECAPTUREMAXSECONDS_KEY,
                15f,
                Locales.VOICECAPTUREMAXSECONDS_DESCRIPTION,
                new AcceptableValueRange<float>(3f, 60f));

            VoiceVadEnergyThreshold = McsConfigApi.RegisterConfig(
                section, order,
                Locales.VOICEVADENERGYTHRESHOLD_KEY,
                0.02f,
                Locales.VOICEVADENERGYTHRESHOLD_DESCRIPTION,
                new AcceptableValueRange<float>(0.001f, 0.5f));

            VoiceVadSilenceSeconds = McsConfigApi.RegisterConfig(
                section, order,
                Locales.VOICEVADSILENCESECONDS_KEY,
                1.2f,
                Locales.VOICEVADSILENCESECONDS_DESCRIPTION,
                new AcceptableValueRange<float>(0.3f, 5f));

            VoiceFeedbackSubtitles = McsConfigApi.RegisterConfig(
                section, order,
                Locales.VOICEFEEDBACKSUBTITLES_KEY,
                true,
                Locales.VOICEFEEDBACKSUBTITLES_DESCRIPTION);

            SttProvider = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTPROVIDER_KEY,
                ESttProvider.None,
                Locales.STTPROVIDER_DESCRIPTION,
                customAttributes: new ConfigurationManagerAttributes
                {
                    CustomDrawer = static entry => McsConfigApi.CustomDrawer(entry,
                        new Dictionary<ESttProvider, string>
                        {
                            { ESttProvider.None, Locales.STTPROVIDERNONE.McsLocalized() },
                            { ESttProvider.OpenAIWhisper, Locales.STTPROVIDEROPENAIWHISPER.McsLocalized() },
                            { ESttProvider.AzureSpeech, Locales.STTPROVIDERAZURESPEECH.McsLocalized() },
                            { ESttProvider.GoogleSpeech, Locales.STTPROVIDERGOOGLESPEECH.McsLocalized() },
                            { ESttProvider.AliyunNls, Locales.STTPROVIDERALIYUNNLS.McsLocalized() },
                            { ESttProvider.TencentAsr, Locales.STTPROVIDERTENCENTASR.McsLocalized() },
                            { ESttProvider.XfyunIat, Locales.STTPROVIDERXFYUNIAT.McsLocalized() },
                            { ESttProvider.VolcIat, Locales.STTPROVIDERVOLCIAT.McsLocalized() },
                            { ESttProvider.BaiduAsr, Locales.STTPROVIDERBAIDUASR.McsLocalized() },
                        }, 2
                    )
                });

            SttApiKey = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTAPIKEY_KEY,
                "",
                Locales.STTAPIKEY_DESCRIPTION);

            SttBaseUrl = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTBASEURL_KEY,
                "",
                Locales.STTBASEURL_DESCRIPTION);

            SttModel = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTMODEL_KEY,
                "",
                Locales.STTMODEL_DESCRIPTION);

            SttLanguage = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTLANGUAGE_KEY,
                "zh-CN",
                Locales.STTLANGUAGE_DESCRIPTION);

            SttTimeoutSec = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTTIMEOUTSEC_KEY,
                15,
                Locales.STTTIMEOUTSEC_DESCRIPTION,
                new AcceptableValueRange<int>(3, 120));

            LlmProvider = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMPROVIDER_KEY,
                ELlmProvider.None,
                Locales.LLMPROVIDER_DESCRIPTION,
                customAttributes: new ConfigurationManagerAttributes
                {
                    CustomDrawer = static entry => McsConfigApi.CustomDrawer(entry,
                        new Dictionary<ELlmProvider, string>
                        {
                            { ELlmProvider.None, Locales.LLMPROVIDERNONE.McsLocalized() },
                            { ELlmProvider.OpenAICompatible, Locales.LLMPROVIDEROPENAICOMPATIBLE.McsLocalized() },
                            { ELlmProvider.Anthropic, Locales.LLMPROVIDERANTHROPIC.McsLocalized() },
                            { ELlmProvider.GoogleGemini, Locales.LLMPROVIDERGOOGLEGEMINI.McsLocalized() },
                            { ELlmProvider.DashScope, Locales.LLMPROVIDERDASHSCOPE.McsLocalized() },
                            { ELlmProvider.Zhipu, Locales.LLMPROVIDERZHIPU.McsLocalized() },
                            { ELlmProvider.Qianfan, Locales.LLMPROVIDERQIANFAN.McsLocalized() },
                            { ELlmProvider.Spark, Locales.LLMPROVIDERSPARK.McsLocalized() },
                            { ELlmProvider.MiniMax, Locales.LLMPROVIDERMINIMAX.McsLocalized() },
                        }, 2
                    )
                });

            LlmApiKey = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMAPIKEY_KEY,
                "",
                Locales.LLMAPIKEY_DESCRIPTION);

            LlmBaseUrl = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMBASEURL_KEY,
                "",
                Locales.LLMBASEURL_DESCRIPTION);

            LlmModel = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMMODEL_KEY,
                "deepseek-v4-flash",
                Locales.LLMMODEL_DESCRIPTION);

            LlmSystemPrompt = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMSYSTEMPROMPT_KEY,
                "",
                Locales.LLMSYSTEMPROMPT_DESCRIPTION);

            LlmTemperature = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMTEMPERATURE_KEY,
                0.2,
                Locales.LLMTEMPERATURE_DESCRIPTION,
                new AcceptableValueRange<double>(0d, 2d));

            LlmMaxTokens = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMMAXTOKENS_KEY,
                3000,
                Locales.LLMMAXTOKENS_DESCRIPTION,
                new AcceptableValueRange<int>(64, 4096));

            LlmTimeoutSec = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMTIMEOUTSEC_KEY,
                15,
                Locales.LLMTIMEOUTSEC_DESCRIPTION,
                new AcceptableValueRange<int>(3, 120));
        }
    }
}