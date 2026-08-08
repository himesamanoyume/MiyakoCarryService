using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>
    /// OpenAI 兼容 Chat Completions 客户端。同时覆盖：
    /// OpenAI / DeepSeek / Moonshot / Together / vLLM / Ollama / LM Studio / LocalAI 等。
    /// 通过 <c>BaseUrl</c> 切换端点，配置项统一为 <c>ApiKey / BaseUrl / Model / SystemPrompt / Temperature / MaxTokens / TimeoutSec</c>。
    /// </summary>
    public sealed class OpenAICompatibleProvider : BaseLlmProvider
    {
        private const string DefaultBaseUrl = "https://api.deepseek.com";
        private const string DefaultModel = "deepseek-v4-flash";

        public override async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return new LlmIntent { Error = "用户文本为空" };
            }

            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;
            var systemPrompt = Tools.BuildSystemPrompt(settings.SystemPrompt);

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
                    var body = BuildChatCompletionsBody(model, systemPrompt, userText, settings.Temperature, settings.MaxTokens);
                    if (useReasoningEffort)
                    {
                        body["reasoning_effort"] = settings.ReasoningEffort;
                    }
                    if (useJsonObject)
                    {
                        body["response_format"] = JObject.FromObject(new { type = "json_object" });
                    }

                    var result = await SendJsonAsync($"{baseUrl}/chat/completions", body, settings, cancellationToken,
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

                    var content = ExtractChatContentText(result.ResponseText);
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

        public override async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            var baseUrl = string.IsNullOrEmpty(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrEmpty(settings.ModelId) ? DefaultModel : settings.ModelId;

            // 最小化连通性测试：不做指令解析，仅取模型回复原文
            var body = BuildChatCompletionsBody(model, "You are a connectivity test. Reply with exactly: pong", "ping", 0d, 64);

            var result = await SendJsonAsync($"{baseUrl}/chat/completions", body, settings, cancellationToken,
                request =>
                {
                    if (!string.IsNullOrEmpty(settings.ApiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                    }
                });
            if (!result.IsSuccess)
            {
                return result.Error;
            }

            var content = ExtractChatContentText(result.ResponseText);
            return string.IsNullOrWhiteSpace(content) ? "(空响应)" : content;
        }
    }
}
