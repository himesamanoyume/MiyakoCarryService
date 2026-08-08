using System;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Providers.Llm
{
    /// <summary>
    /// 服务端 OpenAI 兼容 Chat Completions 客户端，覆盖 OpenAI / DeepSeek / Moonshot / Together /
    /// vLLM / Ollama / LM Studio / LocalAI 等。
    /// </summary>
    public sealed class OpenAICompatibleProvider : BaseLlmProvider
    {
        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? "https://api.deepseek.com" : settings.BaseUrl.TrimEnd('/');
            var modelId = string.IsNullOrEmpty(settings.ModelId) ? "deepseek-v4-flash" : settings.ModelId;
            var maxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 3000;

            try
            {
                // 优先携带 response_format=json_object 以稳定 JSON 输出；
                // 部分 OpenAI 兼容端点（如 OpenCode Zen 的 DeepSeek V4）不支持该参数，会返回 4xx 且错误含
                // "not supported" / "json_object" / "response_format" 等字样，此时去掉该参数回退重试一次。
                // 思考强度 reasoning_effort 同理：default/空不传，不支持的端点去掉后重试。
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    var useJsonObject = attempt == 0;
                    var useReasoningEffort = attempt == 0
                        && !string.IsNullOrEmpty(settings.ReasoningEffort)
                        && settings.ReasoningEffort != "default";
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
                    if (useReasoningEffort)
                    {
                        body["reasoning_effort"] = settings.ReasoningEffort;
                    }
                    if (useJsonObject)
                    {
                        body["response_format"] = JsonSerializer.SerializeToNode(new { type = "json_object" });
                    }

                    var result = await PostJsonAsync($"{baseUrl}/chat/completions", body, settings, cancellationToken,
                        request =>
                        {
                            // 本地端点（Ollama/LM Studio 等）无需 ApiKey，为空时不附加 Authorization
                            if (!string.IsNullOrEmpty(settings.ApiKey))
                            {
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                            }
                        });

                    if (!result.IsSuccess)
                    {
                        // 不支持的端点（错误含 not supported/json_object/response_format/reasoning）去掉可选参数重试一次
                        var unsupported = result.HttpStatus is 400 or 401 or 403 or 422
                            && (result.ErrorBody?.Contains("not supported", StringComparison.OrdinalIgnoreCase) == true
                                || result.ErrorBody?.Contains("json_object", StringComparison.OrdinalIgnoreCase) == true
                                || result.ErrorBody?.Contains("response_format", StringComparison.OrdinalIgnoreCase) == true
                                || result.ErrorBody?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true);
                        if (attempt == 0 && unsupported)
                        {
                            continue;
                        }

                        return new LlmIntent { Error = result.Error };
                    }

                    var json = JsonNode.Parse(result.ResponseText);
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
    }
}