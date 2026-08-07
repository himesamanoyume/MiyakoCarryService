namespace MiyakoCarryService.Server.Models.Llm
{
    /// <summary>
    /// 服务端 LLM 服务商统一配置项。读取自 <c>Assets/configs/mcsconfig.jsonc</c>
    /// 中的 <c>Server</c> 类别下的 <c>Llm*</c> 字段，供所有玩家共用。
    /// </summary>
    public sealed class LlmProviderSettings
    {
        public string ApiKey;
        public string BaseUrl;
        public string ModelId;
        public string SystemPrompt;
        public double Temperature = 0.2;
        public int MaxTokens = 3000;
        public int TimeoutSec = 15;
        /// <summary>LLM 思考强度（reasoning effort）：default/low/medium/high/max，default 或空表示不传参。</summary>
        public string ReasoningEffort = "low";
    }

    /// <summary>
    /// LLM 返回的指令意图。
    /// <list type="bullet">
    ///   <item><c>Order</c> 非空则下护送订单（players/spawnTypeIndex/level/duration）。</item>
    ///   <item><c>Ticket</c> 非空则下罚单减免（percent）。</item>
    ///   <item><c>Renew</c> 非空则为指定护航（昵称/Aid）的订单续订。</item>
    ///   <item><c>Settle</c> 非空则结算指定护航（昵称/Aid）的过期订单。</item>
    ///   <item><c>ReplyText</c> 非空则为纯聊天回复。</item>
    ///   <item><c>Error</c> 非空表示本次意图解析失败。</item>
    /// </list>
    /// </summary>
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
        public int Players;       // 1..4
        public int SpawnTypeIndex; // 0..N-1
        public int Level;        // 1..5
        public int Duration;     // hours, 1..
    }

    public sealed class TicketIntent
    {
        public int Percent; // 1..100
    }

    /// <summary>续订意图：为 <see cref="Target"/>（护航昵称或 Aid）对应的订单发起续订。</summary>
    public sealed class RenewIntent
    {
        public string Target;
    }

    /// <summary>结算意图：结算 <see cref="Target"/>（护航昵称或 Aid）对应的过期订单。</summary>
    public sealed class SettleIntent
    {
        public string Target;
    }
}