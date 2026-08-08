using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Providers.Llm
{
    /// <summary>
    /// 百度千帆 OpenAI 兼容 v2 端点：<c>POST /v2/chat/completions</c>，Bearer ApiKey 鉴权。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class QianfanProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://qianfan.baidubce.com";
        private const string DefaultModel = "ernie-4.5-turbo-128k";

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（千帆 API Key）" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, settings.SystemPrompt, userText, settings.Temperature, settings.MaxTokens);

            var result = await PostJsonAsync($"{baseUrl}/v2/chat/completions", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractChatContentText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = "Qianfan 返回内容为空" };
            }
            return ParseIntentJson(content);
        }
    }
}
