using System.Collections.Generic;
using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Models.Providers
{
    public sealed class XfyunIatResponse
    {
        [JsonProperty("code")]
        public int? Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public XfyunIatResponseData Data { get; set; }
    }

    public sealed class XfyunIatResponseData
    {
        [JsonProperty("result")]
        public XfyunIatResult Result { get; set; }
    }

    public sealed class XfyunIatResult
    {
        [JsonProperty("rg")]
        public List<XfyunIatRg> Rg { get; set; }
    }

    public sealed class XfyunIatRg
    {
        [JsonProperty("v")]
        public string V { get; set; }
    }

    public sealed class GoogleSpeechResponse
    {
        [JsonProperty("results")]
        public List<GoogleSpeechResult> Results { get; set; }
    }

    public sealed class GoogleSpeechResult
    {
        [JsonProperty("alternatives")]
        public List<GoogleSpeechAlternative> Alternatives { get; set; }
    }

    public sealed class GoogleSpeechAlternative
    {
        [JsonProperty("transcript")]
        public string Transcript { get; set; }
    }

    public sealed class BaiduAsrResponse
    {
        [JsonProperty("err_no")]
        public int? ErrNo { get; set; }

        [JsonProperty("err_msg")]
        public string ErrMsg { get; set; }

        [JsonProperty("result")]
        public List<string> Result { get; set; }
    }

    public sealed class BaiduTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }

    public sealed class TencentAsrResponse
    {
        [JsonProperty("Response")]
        public TencentAsrResponseBody Response { get; set; }
    }

    public sealed class TencentAsrResponseBody
    {
        [JsonProperty("Error")]
        public TencentAsrError Error { get; set; }

        [JsonProperty("Result")]
        public string Result { get; set; }
    }

    public sealed class TencentAsrError
    {
        [JsonProperty("Code")]
        public string Code { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }
    }

    public sealed class AliyunTokenResponse
    {
        [JsonProperty("Token")]
        public AliyunToken Token { get; set; }
    }

    public sealed class AliyunToken
    {
        [JsonProperty("Id")]
        public string Id { get; set; }
    }

    public sealed class AliyunNlsResponse
    {
        [JsonProperty("status")]
        public int? Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }
    }

    public sealed class AzureSpeechResponse
    {
        [JsonProperty("RecognitionStatus")]
        public string RecognitionStatus { get; set; }

        [JsonProperty("DisplayText")]
        public string DisplayText { get; set; }
    }

    public sealed class VolcIatResponse
    {
        [JsonProperty("code")]
        public int? Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }
    }

    public sealed class OpenAiSttResponse
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}