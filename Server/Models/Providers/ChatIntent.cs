using System.Text.Json.Serialization;

namespace MiyakoCarryService.Server.Models.Providers
{
    public sealed record McsChatIntent
    {
        [JsonPropertyName("replyText")]
        public string ReplyText { get; set; }

        [JsonPropertyName("order")]
        public OrderIntent Order { get; set; }

        [JsonPropertyName("ticket")]
        public TicketIntent Ticket { get; set; }

        [JsonPropertyName("renew")]
        public RenewIntent Renew { get; set; }

        [JsonPropertyName("settle")]
        public SettleIntent Settle { get; set; }
    }

    public sealed record OrderIntent
    {
        [JsonPropertyName("players")]
        public int? Players { get; set; }

        [JsonPropertyName("spawnTypeIndex")]
        public int? SpawnTypeIndex { get; set; }

        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }
    }

    public sealed record TicketIntent
    {
        [JsonPropertyName("percent")]
        public int? Percent { get; set; }
    }

    public sealed record RenewIntent
    {
        [JsonPropertyName("target")]
        public string Target { get; set; }
    }

    public sealed record SettleIntent
    {
        [JsonPropertyName("target")]
        public string Target { get; set; }
    }
}