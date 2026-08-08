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
    /// 讯飞星火 OpenAI 兼容 HTTP 端点：<c>POST /v1/chat/completions</c>。
    /// 鉴权：Bearer 为星火 APIKey（新版形如 "{apikey}:{apisecret}"）；若 ApiSecret 已单独填写则拼接 "{ApiKey}:{ApiSecret}"。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class SparkProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://spark-api-open.xf-yun.com";
        private const string DefaultModel = "generalv3.5";

        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（星火 APIKey）" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, settings.SystemPrompt, userText, settings.Temperature, settings.MaxTokens);
            var bearer = string.IsNullOrEmpty(settings.ApiSecret) ? settings.ApiKey : $"{settings.ApiKey}:{settings.ApiSecret}";

            var result = await PostJsonAsync($"{baseUrl}/v1/chat/completions", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer));
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractChatContentText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = "Spark 返回内容为空" };
            }
            return ParseIntentJson(content);
        }
    }
}
