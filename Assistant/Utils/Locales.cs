using System.Collections.Generic;
using MiyakoCarryService.Assistant.Enums;

namespace MiyakoCarryService.Assistant.Utils
{
    /// <summary>
    /// Assistant 客户端本地化键集合。键命名约定严格沿用 Client 的 <c>Mcs/&lt;FieldName&gt; Key</c> /
    /// <c>Mcs/&lt;FieldName&gt; Description</c> 风格（参考 <see cref="MiyakoCarryService.Client.Utils.Locales"/>），
    /// 字段名前缀 <c>Assistant</c> 以避免与 Client 同名键冲突。
    /// 服务端 <c>Server/Assets/database/locales/global/*.json</c> 提供翻译副本。
    /// </summary>
    public static class Locales
    {
        // 配置段名（与 Plugin 内 section 字符串保持一致）
        public const string VOICE_SECTION = "Mcs/D.Voice";

        // Each field: 同名 _KEY / _DESCRIPTION 常量
        public const string VOICEENABLED_KEY             = "Mcs/VoiceEnabled Key";
        public const string VOICEENABLED_DESCRIPTION     = "Mcs/VoiceEnabled Description";

        public const string VOICETRIGGERMODE_KEY          = "Mcs/VoiceTriggerMode Key";
        public const string VOICETRIGGERMODE_DESCRIPTION  = "Mcs/VoiceTriggerMode Description";

        public const string VOICEHOTKEY_KEY               = "Mcs/VoiceHotKey Key";
        public const string VOICEHOTKEY_DESCRIPTION        = "Mcs/VoiceHotKey Description";

        public const string VOICECAPTUREMAXSECONDS_KEY       = "Mcs/VoiceCaptureMaxSeconds Key";
        public const string VOICECAPTUREMAXSECONDS_DESCRIPTION = "Mcs/VoiceCaptureMaxSeconds Description";

        public const string VOICEVADENERGYTHRESHOLD_KEY        = "Mcs/VoiceVadEnergyThreshold Key";
        public const string VOICEVADENERGYTHRESHOLD_DESCRIPTION = "Mcs/VoiceVadEnergyThreshold Description";

        public const string VOICEVADSILENCESECONDS_KEY          = "Mcs/VoiceVadSilenceSeconds Key";
        public const string VOICEVADSILENCESECONDS_DESCRIPTION   = "Mcs/VoiceVadSilenceSeconds Description";

        public const string VOICEFEEDBACKSUBTITLES_KEY           = "Mcs/VoiceFeedbackSubtitles Key";
        public const string VOICEFEEDBACKSUBTITLES_DESCRIPTION    = "Mcs/VoiceFeedbackSubtitles Description";

        public const string STTPROVIDER_KEY              = "Mcs/SttProvider Key";
        public const string STTPROVIDER_DESCRIPTION     = "Mcs/SttProvider Description";

        public const string STTAPIKEY_KEY              = "Mcs/SttApiKey Key";
        public const string STTAPIKEY_DESCRIPTION      = "Mcs/SttApiKey Description";

        public const string STTBASEURL_KEY            = "Mcs/SttBaseUrl Key";
        public const string STTBASEURL_DESCRIPTION    = "Mcs/SttBaseUrl Description";

        public const string STTMODEL_KEY              = "Mcs/SttModel Key";
        public const string STTMODEL_DESCRIPTION     = "Mcs/SttModel Description";

        public const string STTLANGUAGE_KEY           = "Mcs/SttLanguage Key";
        public const string STTLANGUAGE_DESCRIPTION  = "Mcs/SttLanguage Description";

        public const string STTTIMEOUTSEC_KEY         = "Mcs/SttTimeoutSec Key";
        public const string STTTIMEOUTSEC_DESCRIPTION = "Mcs/SttTimeoutSec Description";

        public const string LLMPROVIDER_KEY            = "Mcs/LlmProvider Key";
        public const string LLMPROVIDER_DESCRIPTION    = "Mcs/LlmProvider Description";

        public const string LLMAPIKEY_KEY              = "Mcs/LlmApiKey Key";
        public const string LLMAPIKEY_DESCRIPTION     = "Mcs/LlmApiKey Description";

        public const string LLMBASEURL_KEY            = "Mcs/LlmBaseUrl Key";
        public const string LLMBASEURL_DESCRIPTION    = "Mcs/LlmBaseUrl Description";

        public const string LLMMODEL_KEY              = "Mcs/LlmModel Key";
        public const string LLMMODEL_DESCRIPTION      = "Mcs/LlmModel Description";

        public const string LLMSYSTEMPROMPT_KEY        = "Mcs/LlmSystemPrompt Key";
        public const string LLMSYSTEMPROMPT_DESCRIPTION = "Mcs/LlmSystemPrompt Description";

        public const string LLMTEMPERATURE_KEY          = "Mcs/LlmTemperature Key";
        public const string LLMTEMPERATURE_DESCRIPTION  = "Mcs/LlmTemperature Description";

        public const string LLMMAXTOKENS_KEY            = "Mcs/LlmMaxTokens Key";
        public const string LLMMAXTOKENS_DESCRIPTION    = "Mcs/LlmMaxTokens Description";

        public const string LLMTIMEOUTSEC_KEY           = "Mcs/LlmTimeoutSec Key";
        public const string LLMTIMEOUTSEC_DESCRIPTION   = "Mcs/LlmTimeoutSec Description";

        // 运行时反馈
        public const string VOICELISTENING          = "Mcs/VoiceListening";
        public const string VOICETRANSCRIBING       = "Mcs/VoiceTranscribing";
        public const string VOICEINTERPRETING      = "Mcs/VoiceInterpreting";
        public const string VOICEDISPATCHED        = "Mcs/VoiceDispatched";
        public const string VOICEUNHANDLED          = "Mcs/VoiceUnhandled";
        public const string VOICEPROVIDERMISSING    = "Mcs/VoiceProviderMissing";
        public const string VOICESTTFAILED          = "Mcs/VoiceSttFailed";
        public const string VOICELLMFAILED          = "Mcs/VoiceLlmFailed";

        // EVoiceTriggerMode 在 ConfigurationManager 自定义绘制器中显示的本地化文案映射
        public static readonly Dictionary<EVoiceTriggerMode, string> VoiceTriggerModeNames = new()
        {
            { EVoiceTriggerMode.PushToTalk, "Mcs/VoiceTriggerModePushToTalk" },
            { EVoiceTriggerMode.FreeTalk,   "Mcs/VoiceTriggerModeFreeTalk" },
        };

        // ESttProvider 在 ConfigurationManager 自定义绘制器中显示的本地化文案映射
        public static readonly Dictionary<ESttProvider, string> SttProviderNames = new()
        {
            { ESttProvider.None,          "Mcs/SttProviderNone" },
            { ESttProvider.OpenAIWhisper, "Mcs/SttProviderOpenAIWhisper" },
            { ESttProvider.AzureSpeech,    "Mcs/SttProviderAzureSpeech" },
            { ESttProvider.GoogleSpeech,  "Mcs/SttProviderGoogleSpeech" },
            { ESttProvider.AliyunNls,     "Mcs/SttProviderAliyunNls" },
            { ESttProvider.TencentAsr,    "Mcs/SttProviderTencentAsr" },
            { ESttProvider.XfyunIat,      "Mcs/SttProviderXfyunIat" },
            { ESttProvider.VolcIat,       "Mcs/SttProviderVolcIat" },
            { ESttProvider.BaiduAsr,      "Mcs/SttProviderBaiduAsr" },
        };

        // ELlmProvider 在 ConfigurationManager 自定义绘制器中显示的本地化文案映射
        public static readonly Dictionary<ELlmProvider, string> LlmProviderNames = new()
        {
            { ELlmProvider.None,             "Mcs/LlmProviderNone" },
            { ELlmProvider.OpenAICompatible, "Mcs/LlmProviderOpenAICompatible" },
            { ELlmProvider.Anthropic,         "Mcs/LlmProviderAnthropic" },
            { ELlmProvider.GoogleGemini,      "Mcs/LlmProviderGoogleGemini" },
            { ELlmProvider.DashScope,         "Mcs/LlmProviderDashScope" },
            { ELlmProvider.Zhipu,             "Mcs/LlmProviderZhipu" },
            { ELlmProvider.Qianfan,           "Mcs/LlmProviderQianfan" },
            { ELlmProvider.Spark,             "Mcs/LlmProviderSpark" },
            { ELlmProvider.MiniMax,           "Mcs/LlmProviderMiniMax" },
        };
    }
}