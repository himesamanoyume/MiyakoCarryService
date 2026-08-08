using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>阿里云 DashScope 通义千问 Chat Completions。占位实装。</summary>
    public sealed class DashScopeProvider : BaseLlmProvider
    {
        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "DashScopeProvider：通义千问 Chat Completions 桥接待落地（占位）" };
        }
    }
}