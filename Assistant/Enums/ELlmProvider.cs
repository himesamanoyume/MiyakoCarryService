namespace MiyakoCarryService.Assistant.Enums
{
    /// <summary>
    /// LLM 云端服务商。配置项统一为 <c>ApiKey / BaseUrl / Model / SystemPrompt / Temperature / MaxTokens / TimeoutSec</c>；
    /// <c>OpenAICompatible</c> 覆盖 OpenAI / DeepSeek / Moonshot / Together / Ollama / vLLM / LM Studio / LocalAI。
    /// </summary>
    public enum ELlmProvider
    {
        None,
        OpenAICompatible,
        Anthropic,
        GoogleGemini,
        DashScope,
        Zhipu,
        Qianfan,
        Spark,
        MiniMax,
    }
}