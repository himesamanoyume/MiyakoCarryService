using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>
    /// MiniMax v2 Chat Completions：<c>POST /v2/text/chat_completions</c>，Bearer ApiKey 鉴权。
    /// 输出 token 上限用 <c>tokens_to_generate</c>；错误信息在 <c>base_resp</c>。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class MiniMaxProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://api.minimax.chat";
        private const string DefaultModel = "MiniMax-Text-01";

        protected override string ProviderTag
        {
            get { return "MiniMax"; }
        }

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（MiniMax API Key）" };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, systemPrompt, userText, settings.Temperature, settings.MaxTokens, maxTokensFieldName: "tokens_to_generate");

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            // v2 业务错误：base_resp.status_code != 0
            var businessError = CheckBusinessError(result.ResponseText);
            if (businessError != null)
            {
                return new LlmIntent { Error = businessError };
            }

            var content = ExtractChatContentText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = "MiniMax 返回内容为空" };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, "You are a connectivity test. Reply with exactly: pong", "ping", 0d, 64, maxTokensFieldName: "tokens_to_generate");

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractChatContentText(result.ResponseText) ?? result.ResponseText;
        }

        public override Task<PostResponse> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return SendJsonAsync($"{baseUrl}/v2/text/chat_completions", body, settings, cancellationToken, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
        }

        private string CheckBusinessError(string responseString)
        {
            try
            {
                var json = JObject.Parse(responseString);
                var statusCode = json["base_resp"]?["status_code"]?.Value<int>() ?? 0;
                if (statusCode != 0)
                {
                    var statusMsg = json["base_resp"]?["status_msg"]?.ToString() ?? string.Empty;
                    return $"MiniMax 业务错误 {statusCode}: {SafeTrim(statusMsg, 240)}";
                }
            }
            catch
            {
                // 响应非合法 JSON：交由 ParseIntentJson 输出解析错误
            }
            return null;
        }
    }
}
