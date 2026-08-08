using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>Google Gemini <c>generateContent</c> REST。占位实装。</summary>
    public sealed class GoogleGeminiProvider : BaseLlmProvider
    {
        public override async Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "GoogleGeminiProvider：generateContent 桥接待落地（占位）" };
        }
    }
}