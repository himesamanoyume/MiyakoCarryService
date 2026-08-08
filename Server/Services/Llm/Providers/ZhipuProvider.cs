using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>智谱 GLM-4 Chat Completions。占位实装。</summary>
    public sealed class ZhipuProvider : BaseLlmProvider
    {
        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "ZhipuProvider：GLM Chat Completions 桥接待落地（占位）" };
        }
    }
}