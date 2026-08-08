using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Providers.Llm
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

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（MiniMax API Key）" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, settings.SystemPrompt, userText, settings.Temperature, settings.MaxTokens, maxTokensFieldName: "tokens_to_generate");

            var result = await PostJsonAsync($"{baseUrl}/v2/text/chat_completions", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
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

        private string CheckBusinessError(string responseString)
        {
            try
            {
                var node = JsonNode.Parse(responseString);
                var statusCode = node?["base_resp"]?["status_code"]?.GetValue<int>() ?? 0;
                if (statusCode != 0)
                {
                    var statusMsg = node?["base_resp"]?["status_msg"]?.ToString() ?? string.Empty;
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
