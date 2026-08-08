

using System;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Models;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public abstract class BaseSttProvider
    {
        public virtual async Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual string SafeTrim(string s, int max)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        protected virtual string BuildAuthorization(string host, string body, long timestamp, string date, string secretId, string secretKey)
        {
            throw new NotImplementedException();
        }
    }
}