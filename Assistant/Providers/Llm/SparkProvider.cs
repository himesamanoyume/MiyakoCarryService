using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
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

        protected override string ProviderTag
        {
            get { return "Spark"; }
        }

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写（星火 APIKey）" };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, systemPrompt, userText, settings.Temperature, settings.MaxTokens);

            var result = await PostAsync(body, settings, cancellationToken);
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

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, "You are a connectivity test. Reply with exactly: pong", "ping", 0d, 64);
            var result = await PostAsync(body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractChatContentText(result.ResponseText) ?? result.ResponseText;
        }

        private Task<PostResponse> PostAsync(JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var bearer = string.IsNullOrEmpty(settings.ApiSecret) ? settings.ApiKey : $"{settings.ApiKey}:{settings.ApiSecret}";

            return SendJsonAsync($"{baseUrl}/v1/chat/completions", body, settings, cancellationToken,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer));
        }
    }
}
