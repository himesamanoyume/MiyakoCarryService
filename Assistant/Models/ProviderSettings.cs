namespace MiyakoCarryService.Assistant.Models
{
    /// <summary>
    /// 统一的云端服务商配置项集合：所有 STT 和 LLM 服务商共享同一份基本字段，
    /// 国际用户可同时选择国内/国外的任意服务商，仅需填写一份凭证。
    /// </summary>
    public sealed class ProviderSettings
    {
        public string ApiKey;
        public string BaseUrl;
        public string ModelId;
        public string Language;
        public string SystemPrompt;
        public double Temperature = 0.2;
        public int MaxTokens = 3000;
        public int TimeoutSec = 15;
        /// <summary>LLM 思考强度（reasoning effort）：default/low/medium/high/max，default 或空表示不传参。</summary>
        public string ReasoningEffort;
    }
}