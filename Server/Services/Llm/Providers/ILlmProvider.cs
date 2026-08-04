using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Llm;

namespace MiyakoCarryService.Server.Services.Llm.Providers
{
    /// <summary>
    /// 服务端 LLM 服务商适配器接口。所有厂商共享同一份 <see cref="LlmProviderSettings"/> 配置项。
    /// </summary>
    public interface ILlmProvider
    {
        Task<LlmIntent> InterpretAsync(string userText, LlmProviderSettings settings, CancellationToken cancellationToken);
    }
}