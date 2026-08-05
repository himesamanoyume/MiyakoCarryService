using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>
    /// OpenAI 兼容 Chat Completions 客户端。同时覆盖：
    /// OpenAI / DeepSeek / Moonshot / Together / vLLM / Ollama / LM Studio / LocalAI 等。
    /// 通过 <c>BaseUrl</c> 切换端点，配置项统一为 <c>ApiKey / BaseUrl / Model / SystemPrompt / Temperature / MaxTokens / TimeoutSec</c>。
    /// </summary>
    internal sealed class OpenAICompatibleProvider : ILlmProvider
    {
        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.openai.com/v1" : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.Model) ? "gpt-4o-mini" : settings.Model;

            var systemPrompt = PromptTemplates.BuildSystemPrompt(settings.SystemPrompt);
            var body = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userText },
                },
                temperature = settings.Temperature,
                max_tokens = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                response_format = new { type = "json_object" },
            };

            var client = AssistantHttpClient.WithTimeout(settings);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            try
            {
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new LlmIntent { Error = $"OpenAI-Compat HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}" };
                }

                var json = JObject.Parse(responseString);
                var content = json["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new LlmIntent { Error = "OpenAI-Compat 返回内容为空" };
                }

                return ParseIntentJson(content);
            }
            catch (OperationCanceledException)
            {
                return new LlmIntent { Error = "OpenAI-Compat 请求超时" };
            }
            catch (Exception ex)
            {
                return new LlmIntent { Error = $"OpenAI-Compat 异常：{ex.Message}" };
            }
        }

        internal static LlmIntent ParseIntentJson(string content)
        {
            try
            {
                var json = JObject.Parse(content);
                if (json["replyText"] is JToken replyToken && replyToken.Type != JTokenType.Null)
                {
                    var replyText = replyToken.ToString();
                    if (!string.IsNullOrWhiteSpace(replyText))
                    {
                        return new LlmIntent { ReplyText = replyText };
                    }
                }

                var commandName = json.Value<string>("command");
                if (string.IsNullOrWhiteSpace(commandName))
                {
                    return new LlmIntent { Error = "OpenAI-Compat 响应缺少 command 字段" };
                }

                var intent = new LlmIntent { CommandName = commandName };
                var selectorStr = json.Value<string>("selector");
                if (Enum.TryParse<EIntentTargetSelector>(selectorStr, ignoreCase: true, out var selector))
                {
                    intent.Selector = selector;
                }

                if (json["targetIndex"] is JToken idxToken && idxToken.Type != JTokenType.Null)
                {
                    if (idxToken.Type == JTokenType.Integer)
                    {
                        intent.TargetIndex = idxToken.Value<int>();
                    }
                    else if (int.TryParse(idxToken.ToString(), out var parsedIdx))
                    {
                        intent.TargetIndex = parsedIdx;
                    }
                }

                var codeToken = json["targetCodeName"];
                if (codeToken != null && codeToken.Type != JTokenType.Null)
                {
                    intent.TargetCodeName = codeToken.ToString();
                    if (intent.Selector == EIntentTargetSelector.Unspecified && !string.IsNullOrEmpty(intent.TargetCodeName))
                    {
                        intent.Selector = EIntentTargetSelector.ByCodeName;
                    }
                }

                if (intent.Selector == EIntentTargetSelector.Unspecified)
                {
                    intent.Selector = intent.TargetIndex.HasValue
                        ? EIntentTargetSelector.ByIndex
                        : EIntentTargetSelector.All;
                }

                if (json["aimingBodyPart"] is JToken bodyToken && bodyToken.Type != JTokenType.Null)
                {
                    intent.AimingBodyPart = bodyToken.ToString();
                }

                return intent;
            }
            catch (Exception ex)
            {
                return new LlmIntent { Error = $"OpenAI-Compat 解析失败：{ex.Message}；原文：{SafeTrim(content, 240)}" };
            }
        }

        private static string SafeTrim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}