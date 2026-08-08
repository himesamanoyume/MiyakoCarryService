using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>智谱 GLM-4 Chat Completions。占位实装。</summary>
    internal sealed class ZhipuProvider : ILlmProvider
    {
        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "ZhipuProvider：GLM Chat Completions 桥接待落地（占位）" };
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return "PingAsync 未实现";
        }
    }
}