using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Interfaces;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Providers.Llm;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Assistant.Services
{
    /// <summary>
    /// LLM 服务分发器。根据 <see cref="ELlmProvider"/> 选择具体实现，
    /// 配置项通过 <see cref="ProviderSettings"/> 统一传入。
    /// </summary>
    internal sealed class LlmDispatcher
    {
        private readonly ILlmProvider _provider;

        public LlmDispatcher(ELlmProvider type)
        {
            _provider = type switch
            {
                ELlmProvider.OpenAICompatible => new OpenAICompatibleProvider(),
                ELlmProvider.Anthropic => new AnthropicProvider(),
                ELlmProvider.GoogleGemini => new GoogleGeminiProvider(),
                ELlmProvider.DashScope => new DashScopeProvider(),
                ELlmProvider.Zhipu => new ZhipuProvider(),
                ELlmProvider.Qianfan => new QianfanProvider(),
                ELlmProvider.Spark => new SparkProvider(),
                ELlmProvider.MiniMax => new MiniMaxProvider(),
                _ => null,
            };
        }

        public async Task<LlmIntent> InterpretAsync(string userText, ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (_provider == null)
            {
                return new LlmIntent { Error = Locales.LLM_PROVIDER_NOT_CONFIGURED.McsLocalized() };
            }
            return await _provider.InterpretAsync(userText, settings, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> PingAsync(ProviderSettings settings, CancellationToken cancellationToken)
        {
            if (_provider == null)
            {
                return Locales.LLM_PROVIDER_NOT_CONFIGURED.McsLocalized();
            }
            return await _provider.PingAsync(settings, cancellationToken).ConfigureAwait(false);
        }
    }
}