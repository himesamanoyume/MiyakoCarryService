using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Models.Providers
{
    public sealed class XfyunIatRequest
    {
        [JsonProperty("common")]
        public XfyunIatCommon Common { get; set; }

        [JsonProperty("business")]
        public XfyunIatBusiness Business { get; set; }

        [JsonProperty("data")]
        public XfyunIatData Data { get; set; }
    }

    public sealed class XfyunIatCommon
    {
        [JsonProperty("app_id")]
        public string AppId { get; set; }
    }

    public sealed class XfyunIatBusiness
    {
        [JsonProperty("aue")]
        public string Aue { get; set; }

        [JsonProperty("auf")]
        public string Auf { get; set; }

        [JsonProperty("vad_eos")]
        public int? VadEos { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }
    }

    public sealed class XfyunIatData
    {
        [JsonProperty("audio")]
        public string Audio { get; set; }

        [JsonProperty("sample_rate")]
        public int? SampleRate { get; set; }
    }

    public sealed class GoogleSpeechRequest
    {
        [JsonProperty("config")]
        public GoogleSpeechConfig Config { get; set; }

        [JsonProperty("audio")]
        public GoogleSpeechAudio Audio { get; set; }
    }

    public sealed class GoogleSpeechConfig
    {
        [JsonProperty("encoding")]
        public string Encoding { get; set; }

        [JsonProperty("sampleRateHertz")]
        public int? SampleRateHertz { get; set; }

        [JsonProperty("languageCode")]
        public string LanguageCode { get; set; }
    }

    public sealed class GoogleSpeechAudio
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public sealed class BaiduAsrRequest
    {
        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("rate")]
        public int? Rate { get; set; }

        [JsonProperty("channel")]
        public int? Channel { get; set; }

        [JsonProperty("cuid")]
        public string Cuid { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("speech")]
        public string Speech { get; set; }
    }

    public sealed class TencentAsrRequest
    {
        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("SubServiceType")]
        public string SubServiceType { get; set; }

        [JsonProperty("EngSerViceType")]
        public string EngSerViceType { get; set; }

        [JsonProperty("SourceType")]
        public int? SourceType { get; set; }

        [JsonProperty("VoiceFormat")]
        public string VoiceFormat { get; set; }

        [JsonProperty("Data")]
        public string Data { get; set; }

        [JsonProperty("FilterDirty")]
        public int? FilterDirty { get; set; }

        [JsonProperty("FilterModal")]
        public int? FilterModal { get; set; }

        [JsonProperty("ConvertNumMode")]
        public int? ConvertNumMode { get; set; }
    }
}