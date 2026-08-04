using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>Anthropic Claude Messages API <c>/v1/messages</c>。占位实装。</summary>
    public sealed class AnthropicProvider : ILlmProvider
    {
        public async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "AnthropicProvider：Messages API 桥接待落地（占位）" };
        }
    }
}