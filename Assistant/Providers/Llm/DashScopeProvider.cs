using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>阿里云 DashScope 通义千问 Chat Completions。占位实装。</summary>
    internal sealed class DashScopeProvider : ILlmProvider
    {
        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "DashScopeProvider：通义千问 Chat Completions 桥接待落地（占位）" };
        }
    }
}