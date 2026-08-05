using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>
    /// 服务端 OpenAI 兼容 Chat Completions 客户端，覆盖 OpenAI / DeepSeek / Moonshot / Together /
    /// vLLM / Ollama / LM Studio / LocalAI 等。
    /// </summary>
    public sealed class OpenAICompatibleProvider : ILlmProvider
    {
        private static readonly HttpClient SharedClient = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        public async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                return new LlmIntent { Error = "LlmApiKey 未填写" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.deepseek.com" : settings.BaseUrl.TrimEnd('/');
            var modelId = string.IsNullOrEmpty(settings.ModelId) ? "deepseek-v4-flash" : settings.ModelId;
            var maxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 3000;

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
                    var body = new JsonObject
                    {
                        ["model"] = modelId,
                        ["messages"] = JsonSerializer.SerializeToNode(new[]
                        {
                            new { role = "system", content = settings.SystemPrompt ?? "" },
                            new { role = "user", content = userText },
                        }),
                        ["temperature"] = settings.Temperature,
                        ["max_tokens"] = maxTokens,
                    };
                    if (useJsonObject)
                    {
                        body["response_format"] = JsonSerializer.SerializeToNode(new { type = "json_object" });
                    }

                    using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
                    {
                        Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                    using var response = await SharedClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        if (useJsonObject
                            && (int)response.StatusCode is 400 or 401 or 403 or 422
                            && (responseString.Contains("not supported", StringComparison.OrdinalIgnoreCase)
                                || responseString.Contains("json_object", StringComparison.OrdinalIgnoreCase)
                                || responseString.Contains("response_format", StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        return new LlmIntent { Error = $"OpenAI-Compat HTTP {response.StatusCode}: {SafeTrim(responseString, 320)}" };
                    }

                    var json = JsonNode.Parse(responseString);
                    var content = json?["choices"]?[0]?["message"]?["content"]?.ToString();
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

        internal static LlmIntent ParseIntentJson(string content)
        {
            try
            {
                var node = JsonNode.Parse(content);
                if (node?["replyText"] is JsonNode reply && reply.GetValueKind() != JsonValueKind.Null)
                {
                    var replyText = reply.ToString();
                    if (!string.IsNullOrWhiteSpace(replyText))
                    {
                        return new LlmIntent { ReplyText = replyText };
                    }
                }

                if (node?["order"] is JsonNode orderNode)
                {
                    var players = orderNode["players"]?.GetValue<int>() ?? 0;
                    var spawnTypeIndex = orderNode["spawnTypeIndex"]?.GetValue<int>() ?? 0;
                    var level = orderNode["level"]?.GetValue<int>() ?? 0;
                    var duration = orderNode["duration"]?.GetValue<int>() ?? 0;

                    return new LlmIntent
                    {
                        Order = new OrderIntent
                        {
                            Players = players,
                            SpawnTypeIndex = spawnTypeIndex,
                            Level = level,
                            Duration = duration,
                        },
                    };
                }

                if (node?["ticket"] is JsonNode ticketNode)
                {
                    var percent = ticketNode["percent"]?.GetValue<int>() ?? 0;
                    return new LlmIntent
                    {
                        Ticket = new TicketIntent { Percent = percent },
                    };
                }

                return new LlmIntent { Error = "OpenAI-Compat 响应缺少 order/ticket/replyText 字段" };
            }
            catch (Exception ex)
            {
                return new LlmIntent { Error = $"OpenAI-Compat 解析失败：{ex.Message}；原文：{SafeTrim(content, 240)}" };
            }
        }

        private static string SafeTrim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) { return string.Empty; }
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}