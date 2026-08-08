using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>百度 文心 千帆 Chat Completions。占位实装。</summary>
    public sealed class QianfanProvider : BaseLlmProvider
    {
        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "QianfanProvider：文心 千帆 Chat 桥接待落地（占位）" };
        }
    }
}