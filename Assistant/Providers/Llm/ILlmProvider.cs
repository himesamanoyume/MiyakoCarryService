using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Llm
{
    /// <summary>
    /// LLM 服务商适配器接口。输入玩家转写文本，输出意图 JSON（或纯聊天回复）。
    /// 系统提示词由 <see cref="MiyakoCarryService.Assistant.Utils.PromptTemplates"/> 统一生成。
    /// </summary>
    internal interface ILlmProvider
    {
        Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken);

        /// <summary>连通性测试：发送最小化请求，返回模型回复原文（如 "pong"），失败返回描述文本。</summary>
        Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken);
    }
}