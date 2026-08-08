using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>百度 文心 千帆 Chat Completions。占位实装。</summary>
    internal sealed class QianfanProvider : ILlmProvider
    {
        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "QianfanProvider：文心 千帆 Chat 桥接待落地（占位）" };
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return "PingAsync 未实现";
        }
    }
}