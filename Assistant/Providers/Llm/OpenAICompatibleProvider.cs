using System;
using System.Collections.Generic;
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

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.deepseek.com" : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? "deepseek-v4-flash" : settings.ModelId;

            var systemPrompt = PromptTemplates.BuildSystemPrompt(settings.SystemPrompt);
            var client = AssistantHttpClient.WithTimeout();

            // 请求级超时：与商人侧实现一致，互不干扰
            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                // 优先携带 response_format=json_object 以稳定 JSON 输出；
                // 部分 OpenAI 兼容端点（如 OpenCode Zen 的 DeepSeek V4）不支持该参数，会返回 4xx 且错误含
                // "not supported" / "json_object" / "response_format" 等字样，此时去掉该参数回退重试一次。
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    var useJsonObject = attempt == 0;
                    // 思考强度：default/空不传；不支持的端点报 400 且错误含 reasoning/not supported 时去掉重试
                    var useReasoningEffort = attempt == 0
                        && !string.IsNullOrEmpty(settings.ReasoningEffort)
                        && settings.ReasoningEffort != "default";
                    var body = new JObject
                    {
                        ["model"] = model,
                        ["messages"] = JArray.FromObject(new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = userText },
                        }),
                        ["temperature"] = settings.Temperature,
                        ["max_tokens"] = settings.MaxTokens > 0 ? settings.MaxTokens : 3000,
                    };
                    if (useReasoningEffort)
                    {
                        body["reasoning_effort"] = settings.ReasoningEffort;
                    }
                    if (useJsonObject)
                    {
                        body["response_format"] = JObject.FromObject(new { type = "json_object" });
                    }

                    using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
                    };
                    // 本地端点（Ollama/LM Studio 等）无需 ApiKey，为空时不附加 Authorization
                    if (!string.IsNullOrEmpty(settings.ApiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                    }

                    using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        // 不支持的端点（错误含 not supported/json_object/response_format/reasoning）去掉可选参数重试一次
                        var unsupported = (int)response.StatusCode is 400 or 401 or 403 or 422
                            && (responseString.Contains("not supported", StringComparison.OrdinalIgnoreCase)
                                || responseString.Contains("json_object", StringComparison.OrdinalIgnoreCase)
                                || responseString.Contains("response_format", StringComparison.OrdinalIgnoreCase)
                                || responseString.Contains("reasoning", StringComparison.OrdinalIgnoreCase));
                        if (attempt == 0 && unsupported)
                        {
                            continue;
                        }

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

                return new LlmIntent { Error = "OpenAI-Compat 请求失败（重试后仍被拒绝）" };
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

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.deepseek.com" : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? "deepseek-v4-flash" : settings.ModelId;
            var client = AssistantHttpClient.WithTimeout();

            var timeout = settings.TimeoutSec > 0 ? TimeSpan.FromSeconds(settings.TimeoutSec) : TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                // 最小化连通性测试：不做指令解析，仅取模型回复原文
                var body = new JObject
                {
                    ["model"] = model,
                    ["messages"] = JArray.FromObject(new[]
                    {
                        new { role = "system", content = "You are a connectivity test. Reply with exactly: pong" },
                        new { role = "user", content = "ping" },
                    }),
                    ["temperature"] = 0d,
                    ["max_tokens"] = 64,
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"),
                };
                if (!string.IsNullOrEmpty(settings.ApiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                }

                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return $"HTTP {response.StatusCode}: {SafeTrim(responseString, 240)}";
                }

                var json = JObject.Parse(responseString);
                var content = json["choices"]?[0]?["message"]?["content"]?.ToString();
                return string.IsNullOrWhiteSpace(content) ? "(空响应)" : content;
            }
            catch (OperationCanceledException)
            {
                return "请求超时";
            }
            catch (Exception ex)
            {
                return $"异常：{ex.Message}";
            }
        }

        internal static LlmIntent ParseIntentJson(string content)
        {
            try
            {
                var json = JObject.Parse(content);
                // 识别结果只允许是指令：LLM 返回 replyText/error 一律视为未识别
                if (json["replyText"] is JToken replyToken
                    && replyToken.Type != JTokenType.Null
                    && !string.IsNullOrWhiteSpace(replyToken.ToString()))
                {
                    return new LlmIntent { Error = LlmIntent.NotRecognized };
                }
                if (json["error"] is JToken errToken
                    && errToken.Type != JTokenType.Null
                    && !string.IsNullOrWhiteSpace(errToken.ToString()))
                {
                    return new LlmIntent { Error = LlmIntent.NotRecognized };
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

                // 多目标：targetIndices / targetCodeNames 数组（优先于单值字段）
                if (json["targetIndices"] is JToken idxArrToken && idxArrToken.Type == JTokenType.Array)
                {
                    var indices = new List<int>();
                    foreach (var item in idxArrToken)
                    {
                        if (item.Type == JTokenType.Integer)
                        {
                            indices.Add(item.Value<int>());
                        }
                        else if (int.TryParse(item.ToString(), out var parsedArrIdx))
                        {
                            indices.Add(parsedArrIdx);
                        }
                    }
                    if (indices.Count > 0)
                    {
                        intent.TargetIndices = indices;
                    }
                }

                if (json["targetCodeNames"] is JToken codeArrToken && codeArrToken.Type == JTokenType.Array)
                {
                    var codeNames = new List<string>();
                    foreach (var item in codeArrToken)
                    {
                        var s = item.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            codeNames.Add(s);
                        }
                    }
                    if (codeNames.Count > 0)
                    {
                        intent.TargetCodeNames = codeNames;
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
                    intent.Selector = intent.TargetIndices != null || intent.TargetIndex.HasValue
                        ? EIntentTargetSelector.ByIndex
                        : intent.TargetCodeNames != null || !string.IsNullOrEmpty(intent.TargetCodeName)
                            ? EIntentTargetSelector.ByCodeName
                            : EIntentTargetSelector.All;
                }

                if (json["aimingBodyPart"] is JToken bodyToken && bodyToken.Type != JTokenType.Null)
                {
                    intent.AimingBodyPart = bodyToken.ToString();
                }

                if (json["optionIndex"] is JToken optToken && optToken.Type != JTokenType.Null)
                {
                    if (optToken.Type == JTokenType.Integer)
                    {
                        intent.OptionIndex = optToken.Value<int>();
                    }
                    else if (int.TryParse(optToken.ToString(), out var parsedOpt))
                    {
                        intent.OptionIndex = parsedOpt;
                    }
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
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}