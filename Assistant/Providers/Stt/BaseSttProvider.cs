

using System;
using System.Threading;
using System.Threading.Tasks;
using MiyakoCarryService.Assistant.Interfaces;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Assistant.Utils;
using MiyakoCarryService.Client.Extensions;
using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Providers.Stt
{
    public abstract class BaseSttProvider : BaseProvider, ISttProvider
    {
        protected const int RequiredRate = 16000;

        protected TResponse ParseResponseJson<TResponse>(PostResponse result)
            where TResponse : class
        {
            try
            {
                return JsonConvert.DeserializeObject<TResponse>(result.ResponseText);
            }
            catch
            {
                return null;
            }
        }

        public virtual Task<SttResult> TranscribeAsync(AudioSegment audio, ProviderSettings settings, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SttResult { Error = Locales.ERROR_NOT_IMPLEMENTED.McsLocalized() });
        }

        protected SttResult ValidateAudio(AudioSegment audio)
        {
            if (audio == null || audio.LengthSamples == 0)
            {
                return new SttResult { Error = Locales.STT_AUDIO_EMPTY.McsLocalized() };
            }
            return null;
        }

        protected bool TryPrepareWav(AudioSegment audio, out byte[] wavBytes, out string error)
        {
            var invalid = ValidateAudio(audio);
            if (invalid != null)
            {
                wavBytes = null;
                error = invalid.Error;
                return false;
            }

            wavBytes = Tools.Encode(audio.Samples, audio.SampleRate, audio.Channels);
            if (wavBytes.Length == 0)
            {
                wavBytes = null;
                error = Locales.STT_WAV_ENCODE_FAILED.McsLocalized();
                return false;
            }

            error = null;
            return true;
        }

        protected bool TryPrepare16kWav(AudioSegment audio, out byte[] wavBytes, out string error)
        {
            var invalid = ValidateAudio(audio);
            if (invalid != null)
            {
                wavBytes = null;
                error = invalid.Error;
                return false;
            }

            var rate = audio.SampleRate;
            var samples = audio.Samples;
            if (rate != RequiredRate)
            {
                samples = Tools.Resample(samples, rate, RequiredRate);
                rate = RequiredRate;
            }
            wavBytes = Tools.Encode(samples, rate, 1);
            if (wavBytes.Length == 0)
            {
                wavBytes = null;
                error = Locales.STT_WAV_ENCODE_FAILED.McsLocalized();
                return false;
            }

            error = null;
            return true;
        }
    }
}
