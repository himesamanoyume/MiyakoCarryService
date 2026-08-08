using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>MiniMax 海螺 Chat Completions。占位实装。</summary>
    public sealed class MiniMaxProvider : BaseLlmProvider
    {
        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "MiniMaxProvider：海螺 Chat 桥接待落地（占位）" };
        }
    }
}