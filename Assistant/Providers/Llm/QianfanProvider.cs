using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>
    /// 百度千帆 OpenAI 兼容 v2 端点：<c>POST /v2/chat/completions</c>，Bearer ApiKey 鉴权。
    /// 意图解析复用 OpenAI 兼容的 JSON schema。
    /// </summary>
    public sealed class QianfanProvider : BaseLlmProvider
    {
        protected override string ProviderDisplayName => Locales.LLMPROVIDERQIANFAN.McsLocalized();

        private const string DefaultBaseUrl = "https://qianfan.baidubce.com";
        private const string DefaultModel = "ernie-4.5-turbo-128k";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = Locales.LLM_USER_TEXT_EMPTY.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_APIKEY_MISSING.McsLocalized(), "千帆 API Key") };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, systemPrompt, userText, settings.Temperature, settings.MaxTokens);

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractChatContentText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_EMPTY_CONTENT.McsLocalized(), ProviderDisplayName) };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var body = BuildChatCompletionsBody(model, "You are a connectivity test. Reply with exactly: pong", "ping", 0d, 64);
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
            return SendJsonAsync($"{baseUrl}/v2/chat/completions", body, settings, cancellationToken, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey));
        }
    }
}
