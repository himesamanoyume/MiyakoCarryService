using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>
    /// Anthropic Claude Messages API <c>/v1/messages</c>。OpenAI-Compat 不直接兼容，
    /// 此处保留占位以便后续按官方 SDK 落地。
    /// </summary>
    internal sealed class AnthropicProvider : ILlmProvider
    {
        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "AnthropicProvider：Messages API 桥接待落地（占位）" };
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return "PingAsync 未实现";
        }
    }
}