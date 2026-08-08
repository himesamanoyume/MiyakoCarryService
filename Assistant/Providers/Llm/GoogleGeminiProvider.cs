using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>Google Gemini <c>generateContent</c> REST。占位实装，后续按官方 SDK 落地。</summary>
    internal sealed class GoogleGeminiProvider : ILlmProvider
    {
        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new LlmIntent { Error = "GoogleGeminiProvider：generateContent 桥接待落地（占位）" };
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return "PingAsync 未实现";
        }
    }
}