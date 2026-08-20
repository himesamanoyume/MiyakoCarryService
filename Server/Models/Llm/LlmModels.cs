using System;
using System.Threading;

namespace MiyakoCarryService.Server.Models.Llm
{
    public sealed class LlmProviderSettings
    {
        public string ApiKey;
        public string ApiSecret;
        public string BaseUrl;
        public string ModelId;
        public string SystemPrompt;
        public double Temperature = 0.2;
        public int MaxTokens = 3000;
        public int TimeoutSec = 15;
        public string ReasoningEffort = "none";
    }

    public sealed class LlmIntent
    {
        public OrderIntent Order;
        public TicketIntent Ticket;
        public RenewIntent Renew;
        public SettleIntent Settle;
        public string ReplyText;
        public string Error;

        public bool IsReply { get { return !string.IsNullOrEmpty(ReplyText); } }
        public bool IsError { get { return !string.IsNullOrEmpty(Error); } }
        public bool IsCommand { get { return Order != null || Ticket != null || Renew != null || Settle != null; } }
    }

    public sealed class OrderIntent
    {
        public int Players;
        public int SpawnTypeIndex;
        public int Level;
        public int Duration;
    }

    public sealed class TicketIntent
    {
        public int Percent;
    }

    public sealed class RenewIntent
    {
        public string Target;
    }

    public sealed class SettleIntent
    {
        public string Target;
    }

    public readonly struct LlmDispatchResult
    {
        public bool IsHandled { get; }

        private LlmDispatchResult(bool handled)
        {
            IsHandled = handled;
        }

        public static LlmDispatchResult Handled()
        {
            return new LlmDispatchResult(true);
        }

        public static LlmDispatchResult NotHandled()
        {
            return new LlmDispatchResult(false);
        }
    }

    public sealed class RateBucket
    {
        private readonly int _maxPerMinute;
        private int _consumed;
        private long _windowStartTicks;

        public RateBucket(int maxPerMinute)
        {
            _maxPerMinute = maxPerMinute;
            _windowStartTicks = DateTime.UtcNow.Ticks;
        }

        public bool TryConsume()
        {
            var now = DateTime.UtcNow.Ticks;
            var windowTicks = TimeSpan.FromMinutes(1).Ticks;
            var start = Interlocked.Read(ref _windowStartTicks);
            if (now - start >= windowTicks)
            {
                if (Interlocked.CompareExchange(ref _windowStartTicks, now, start) == start)
                {
                    Interlocked.Exchange(ref _consumed, 0);
                }
            }

            var consumed = Interlocked.Increment(ref _consumed);
            return consumed <= _maxPerMinute;
        }
    }
}