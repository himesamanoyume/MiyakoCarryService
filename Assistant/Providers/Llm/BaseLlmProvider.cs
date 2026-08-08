
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Interfaces;
using MiyakoCarryService.Assistant.Models;
using Newtonsoft.Json.Linq;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    public abstract class BaseLlmProvider : BaseProvider, ILlmProvider
    {
        public virtual Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return Task.FromResult(new LlmIntent { Error = "此接口未实现" });
        }

        public virtual Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            return Task.FromResult("该服务商不支持 Ping 测试");
        }

        /// <summary>
        /// 提取 OpenAI 兼容响应中 <c>choices[0].message.content</c> 的文本；
        /// 解析失败或内容为空时返回 null。
        /// </summary>
        protected string ExtractChatContentText(string responseString)
        {
            try
            {
                var json = JObject.Parse(responseString);
                return json["choices"]?[0]?["message"]?["content"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 构造 OpenAI 兼容 Chat Completions 请求体（model/messages/temperature/max_tokens）。
        /// <paramref name="maxTokensFieldName"/> 可定制输出 token 上限字段名（如 MiniMax 的 tokens_to_generate）。
        /// </summary>
        protected JObject BuildChatCompletionsBody(string model, string systemPrompt, string userText, double temperature, int maxTokens, string maxTokensFieldName = "max_tokens")
        {
            var messages = new JArray
            {
                new JObject { ["role"] = "system", ["content"] = systemPrompt },
                new JObject { ["role"] = "user", ["content"] = userText },
            };
            return new JObject
            {
                ["model"] = model,
                ["messages"] = messages,
                ["temperature"] = temperature,
                [maxTokensFieldName] = maxTokens > 0 ? maxTokens : 3000,
            };
        }

        public LlmIntent ParseIntentJson(string content)
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
                        intent.Selector = EIntentTargetSelector.ByName;
                    }
                }

                if (intent.Selector == EIntentTargetSelector.Unspecified)
                {
                    intent.Selector = intent.TargetIndices != null || intent.TargetIndex.HasValue
                        ? EIntentTargetSelector.ByIndex
                        : intent.TargetCodeNames != null || !string.IsNullOrEmpty(intent.TargetCodeName)
                            ? EIntentTargetSelector.ByName
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

        public virtual Task<PostResponse> PostAsync(string baseUrl, JObject body, ProviderSettings settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
