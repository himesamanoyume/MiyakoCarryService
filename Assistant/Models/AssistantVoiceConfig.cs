using BepInEx.Configuration;
using MiyakoCarryService.Assistant.Enums;

namespace MiyakoCarryService.Assistant.Models
{
    /// <summary>
    /// Assistant 客户端的语音配置项快照。由 Plugin 的 <see cref="ConfigEntry{T}"/> 字段在 Start 时填充，
    /// 调用站点可以一次性读取，不必每次都访问 ConfigEntry 检索字典。
    /// </summary>
    public sealed class AssistantVoiceConfig
    {
        public bool Enabled;
        public EVoiceTriggerMode TriggerMode;
        public KeyboardShortcut HotKey;
        public float CaptureMaxSeconds;
        public float VadEnergyThreshold;
        public float VadSilenceSeconds;
        public bool FeedbackSubtitles;

        public ESttProvider SttProvider;
        public ProviderSettings SttSettings;

        public ELlmProvider LlmProvider;
        public ProviderSettings LlmSettings;

        public static AssistantVoiceConfig FromConfig()
        {
            return new AssistantVoiceConfig
            {
                Enabled = MiyakoCarryServiceAssistantPlugin.VoiceEnabled.Value,
                TriggerMode = MiyakoCarryServiceAssistantPlugin.VoiceTriggerMode.Value,
                HotKey = MiyakoCarryServiceAssistantPlugin.VoiceHotKey.Value,
                CaptureMaxSeconds = MiyakoCarryServiceAssistantPlugin.VoiceCaptureMaxSeconds.Value,
                VadEnergyThreshold = MiyakoCarryServiceAssistantPlugin.VoiceVadEnergyThreshold.Value,
                VadSilenceSeconds = MiyakoCarryServiceAssistantPlugin.VoiceVadSilenceSeconds.Value,
                FeedbackSubtitles = MiyakoCarryServiceAssistantPlugin.VoiceFeedbackSubtitles.Value,
                SttProvider = MiyakoCarryServiceAssistantPlugin.SttProvider.Value,
                SttSettings = new ProviderSettings
                {
                    ApiKey = MiyakoCarryServiceAssistantPlugin.SttApiKey.Value,
                    BaseUrl = MiyakoCarryServiceAssistantPlugin.SttBaseUrl.Value,
                    ModelId = MiyakoCarryServiceAssistantPlugin.SttModelId.Value,
                    Language = MiyakoCarryServiceAssistantPlugin.SttLanguage.Value,
                    TimeoutSec = MiyakoCarryServiceAssistantPlugin.SttTimeoutSec.Value,
                },
                LlmProvider = MiyakoCarryServiceAssistantPlugin.LlmProvider.Value,
                LlmSettings = new ProviderSettings
                {
                    ApiKey = MiyakoCarryServiceAssistantPlugin.LlmApiKey.Value,
                    BaseUrl = MiyakoCarryServiceAssistantPlugin.LlmBaseUrl.Value,
                    ModelId = MiyakoCarryServiceAssistantPlugin.LlmModelId.Value,
                    SystemPrompt = MiyakoCarryServiceAssistantPlugin.LlmSystemPrompt.Value,
                    Temperature = MiyakoCarryServiceAssistantPlugin.LlmTemperature.Value,
                    MaxTokens = MiyakoCarryServiceAssistantPlugin.LlmMaxTokens.Value,
                    TimeoutSec = MiyakoCarryServiceAssistantPlugin.LlmTimeoutSec.Value,
                },
            };
        }

        public AssistantVoiceConfig Refreshed()
        {
            // 配置项可被玩家在 ConfigurationManager 中实时修改；舰载阶段每次都 refetch。
            // 为避免热键/模式切换需要重启，统一每次任务开始时刷新一份。
            var refreshed = FromConfig();
            return refreshed;
        }
    }
}