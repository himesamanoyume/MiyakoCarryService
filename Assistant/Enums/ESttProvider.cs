namespace MiyakoCarryService.Assistant.Enums
{
    /// <summary>
    /// STT 云端服务商。配置项统一为 <c>ApiKey / BaseUrl / Model / Language / TimeoutSec</c>；
    /// 各服务商在 <see cref="MiyakoCarryService.Assistant.Services.SttDispatcher"/> 中按此枚举选择实现。
    /// </summary>
    public enum ESttProvider
    {
        None,
        OpenAIWhisper,
        AzureSpeech,
        GoogleSpeech,
        AliyunNls,
        TencentAsr,
        XfyunIat,
        VolcIat,
        BaiduAsr,
    }
}