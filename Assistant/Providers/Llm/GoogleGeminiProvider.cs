using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    public sealed class GoogleGeminiProvider : BaseLlmProvider
    {
        protected override string ProviderDisplayName => Locales.LLMPROVIDERGOOGLEGEMINI.McsLocalized();

        private const string DefaultBaseUrl = "https://generativelanguage.googleapis.com";
        private const string DefaultModel = "gemini-2.0-flash";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = Locales.LLM_USER_TEXT_EMPTY.McsLocalized() };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_APIKEY_MISSING.McsLocalized(), "Gemini API Key") };
            }

            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);
            var body = new JObject
            {
                ["system_instruction"] = new JObject { ["parts"] = JArray.FromObject(new[] { new { text = systemPrompt } }) },
                ["contents"] = JArray.FromObject(new[] { new { role = "user", parts = new[] { new { text = userText } } } }),
                ["generationConfig"] = new JObject
                {
                    ["temperature"] = settings.Temperature,
                    ["maxOutputTokens"] = settings.MaxTokens > 0 ? settings.MaxTokens : 10107,
                },
            };

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return new LlmIntent { Error = result.Error };
            }

            var content = ExtractText(result.ResponseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmIntent { Error = string.Format(Locales.LLM_EMPTY_CONTENT.McsLocalized(), ProviderDisplayName) };
            }
            return ParseIntentJson(content);
        }

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var body = new JObject
            {
                ["system_instruction"] = new JObject { ["parts"] = JArray.FromObject(new[] { new { text = "You are a connectivity test. Reply with exactly: pong" } }) },
                ["contents"] = JArray.FromObject(new[] { new { role = "user", parts = new[] { new { text = "ping" } } } }),
                ["generationConfig"] = new JObject { ["temperature"] = 0d, ["maxOutputTokens"] = 64 },
            };

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var result = await PostAsync(baseUrl, body, settings, cancellationToken);
            if (!result.IsSuccess)
            {
                return result.Error;
            }
            return ExtractText(result.ResponseText) ?? result.ResponseText;
        }

        public override Task<PostResponse> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var endpoint = $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}";
            return SendJsonAsync(endpoint, body, settings, cancellationToken);
        }

        private string ExtractText(string responseString)
        {
            try
            {
                var json = JObject.Parse(responseString);
                var sb = new StringBuilder();
                if (json["candidates"]?[0]?["content"]?["parts"] is JArray parts)
                {
                    foreach (var part in parts)
                    {
                        var text = part?["text"]?.ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            sb.Append(text);
                        }
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
