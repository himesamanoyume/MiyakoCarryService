using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Mgrs;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Services;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

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

        #region Assistant

        public static ConfigEntry<bool> VoiceEnabled;
        public static ConfigEntry<EVoiceTriggerMode> VoiceTriggerMode;
        public static ConfigEntry<KeyboardShortcut> VoiceHotKey;
        public static ConfigEntry<float> VoiceCaptureMaxSeconds;
        public static ConfigEntry<float> VoiceVadEnergyThreshold;
        public static ConfigEntry<float> VoiceVadSilenceSeconds;
        public static ConfigEntry<bool> VoiceFeedbackSubtitles;
        public static ConfigEntry<string> RecordDevice;
        public static ConfigEntry<string> HttpProxyHost;
        public static ConfigEntry<string> HttpProxyPort;
        public static ConfigEntry<ESttProvider> SttProvider;
        public static ConfigEntry<string> SttApiKey;
        public static ConfigEntry<string> SttBaseUrl;
        public static ConfigEntry<string> SttModelId;
        public static ConfigEntry<string> SttLanguage;
        public static ConfigEntry<int> SttTimeoutSec;
        public static ConfigEntry<ELlmProvider> LlmProvider;
        public static ConfigEntry<string> LlmApiKey;
        public static ConfigEntry<string> LlmBaseUrl;
        public static ConfigEntry<string> LlmModelId;
        public static ConfigEntry<string> LlmSystemPrompt;
        public static ConfigEntry<double> LlmTemperature;
        public static ConfigEntry<int> LlmMaxTokens;
        public static ConfigEntry<int> LlmTimeoutSec;

        public static ConfigEntry<bool> SttDebugEnabled;
        public static ConfigEntry<string> SttDebugText;
        public static ConfigEntry<bool> LlmDebugSend;
        public static ConfigEntry<string> LlmDebugResult;

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
            AssistantHttpClient.Initialize();

            McsEventApi.Notify(new GameLoopMgrEnableEvent
            {
                MgrTypes = [typeof(VoiceMgr)]
            });
        }

        private void SetupConfig()
        {
            #region ASSISTANT

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

            var recordDevices = new List<string> { "Default" };
            if (Microphone.devices != null)
            {
                recordDevices.AddRange(Microphone.devices);
            }

            RecordDevice = McsConfigApi.RegisterConfig(
                section, order,
                Locales.RECORDDEVICE_KEY,
                "Default",
                Locales.RECORDDEVICE_DESCRIPTION,
                new AcceptableValueList<string>(recordDevices.ToArray()));

            SttProvider = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTPROVIDER_KEY,
                ESttProvider.None,
                Locales.STTPROVIDER_DESCRIPTION,
                customAttributes: new ConfigurationManagerAttributes
                {
                    CustomDrawer = static entry => McsConfigApi.DropdownDrawer(entry,
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
                        }
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

            SttModelId = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTMODELID_KEY,
                "",
                Locales.STTMODELID_DESCRIPTION);

            SttLanguage = McsConfigApi.RegisterConfig(
                section, order,
                Locales.STTLANGUAGE_KEY,
                "",
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
                    CustomDrawer = static entry => McsConfigApi.DropdownDrawer(entry,
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
                        }
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

            LlmModelId = McsConfigApi.RegisterConfig(
                section, order,
                Locales.LLMMODELID_KEY,
                "deepseek-v4-flash",
                Locales.LLMMODELID_DESCRIPTION);

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

            HttpProxyHost = McsConfigApi.RegisterConfig(
                section, order,
                Locales.HTTPPROXYHOST_KEY,
                "",
                Locales.HTTPPROXYHOST_DESCRIPTION);

            HttpProxyPort = McsConfigApi.RegisterConfig(
                section, order,
                Locales.HTTPPROXYPORT_KEY,
                "",
                Locales.HTTPPROXYPORT_DESCRIPTION);

            #endregion
            #region DEBUG

            const int debugOrder = 2000;

            SttDebugEnabled = McsConfigApi.RegisterConfig(
                Client.Utils.Locales.DEBUG, debugOrder,
                Locales.STTDEBUGENABLED_KEY,
                false,
                Locales.STTDEBUGENABLED_DESCRIPTION,
                needNotify: false);

            SttDebugText = McsConfigApi.RegisterConfig(
                Client.Utils.Locales.DEBUG, debugOrder,
                Locales.STTDEBUGTEXT_KEY,
                "",
                Locales.STTDEBUGTEXT_DESCRIPTION,
                needNotify: false,
                customAttributes: new ConfigurationManagerAttributes
                {
                    CustomDrawer = DrawDebugReadonlyText,
                    HideDefaultButton = true,
                });

            LlmDebugSend = McsConfigApi.RegisterConfig(
                Client.Utils.Locales.DEBUG, debugOrder,
                Locales.LLMDEBUGSEND_KEY,
                false,
                Locales.LLMDEBUGSEND_DESCRIPTION,
                needNotify: false,
                customAttributes: new ConfigurationManagerAttributes
                {
                    CustomDrawer = static entry =>
                    {
                        if (GUILayout.Button(Locales.LLMDEBUGSEND_KEY.McsLocalized(), GUILayout.ExpandWidth(true)))
                        {
                            _ = RunLlmDebugTestAsync();
                        }
                    },
                    HideDefaultButton = true,
                });

            LlmDebugResult = McsConfigApi.RegisterConfig(
                Client.Utils.Locales.DEBUG, debugOrder,
                Locales.LLMDEBUGRESULT_KEY,
                "",
                Locales.LLMDEBUGRESULT_DESCRIPTION,
                needNotify: false,
                customAttributes: new ConfigurationManagerAttributes
                {
                    CustomDrawer = DrawDebugReadonlyText,
                    HideDefaultButton = true,
                });

            #endregion
        }

        private static GUIStyle _debugReadonlyStyle;

        private static void DrawDebugReadonlyText(ConfigEntryBase entry)
        {
            _debugReadonlyStyle ??= new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                stretchWidth = true,
            };
            GUILayout.Label((string)entry.BoxedValue ?? "", _debugReadonlyStyle);
        }

        /// <summary>
        /// 使用当前 LLM 配置发送测试内容（默认取 STT 调试文本，为空则 "ping"），
        /// 回复或报错信息覆盖写入 LLM 返回结果。
        /// </summary>
        private static async Task RunLlmDebugTestAsync()
        {
            // 请求开始前先显示请求中状态，结果返回后覆盖
            LlmDebugResult.Value = "正在请求";
            try
            {
                var settings = new ProviderSettings
                {
                    ApiKey = LlmApiKey.Value,
                    BaseUrl = LlmBaseUrl.Value,
                    ModelId = LlmModelId.Value,
                    SystemPrompt = LlmSystemPrompt.Value,
                    Temperature = LlmTemperature.Value,
                    MaxTokens = LlmMaxTokens.Value,
                    // 调试测试放宽超时：推理模型（如 DeepSeek V4 Flash）思考耗时较长，15s 默认值容易误判超时
                    TimeoutSec = Math.Max(LlmTimeoutSec.Value, 60),
                };

                var text = SttDebugText?.Value;
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = "ping";
                }

                var dispatcher = new LlmDispatcher(LlmProvider.Value);
                var intent = await dispatcher.InterpretAsync(text, settings, System.Threading.CancellationToken.None).ConfigureAwait(true);

                if (intent == null || intent.IsError)
                {
                    LlmDebugResult.Value = $"错误：{intent?.Error ?? "null"}";
                }
                else if (!string.IsNullOrEmpty(intent.ReplyText))
                {
                    LlmDebugResult.Value = intent.ReplyText;
                }
                else if (!string.IsNullOrEmpty(intent.CommandName))
                {
                    LlmDebugResult.Value = $"指令：{intent.CommandName}";
                }
                else
                {
                    LlmDebugResult.Value = "空响应";
                }
            }
            catch (Exception ex)
            {
                LlmDebugResult.Value = $"错误：{ex.Message}";
            }
        }
    }
}