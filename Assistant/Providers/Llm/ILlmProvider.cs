using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    internal interface ILlmProvider
    {
        Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken);
        Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken);
    }
}