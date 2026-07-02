using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record McsPluginClientConfig
    {
        [JsonPropertyName("BalanceRestriction")]
        public bool BalanceRestriction { get; set; } = false;
    }

    public record McsPluginServerConfig
    {
        [JsonPropertyName("CheckUpdate")]
        public bool CheckUpdate { get; set; } = true;

        [JsonPropertyName("CheckIfdian")]
        public bool CheckIfdian { get; set; } = true;

        [JsonPropertyName("TicketPricePerPercent")]
        public int TicketPricePerPercent 
        { 
            get;
            set
            {
                if (value < 0)
                {
                    field = 0;
                }
                field = value;
            }
        } = 300000;

        [JsonPropertyName("PunishmentMultiMax")]
        public double PunishmentMultiMax
        { 
            get;
            set
            {
                if (value < 0)
                {
                    field = 0;
                }
                field = value;
            }
        } = 1d;

        [JsonPropertyName("OrderPendingPaymentTime")]
        public long OrderPendingPaymentTime 
        { 
            get;
            set
            {
                if (value < 0)
                {
                    field = 0;
                }
                field = value;
            }
        } = 900;

        [JsonPropertyName("CompensationPrice")]
        public double CompensationPrice 
        { 
            get;
            set
            {
                if (value < 0)
                {
                    field = 0;
                }
                field = value;
            }
        } = 300000d;

        [JsonPropertyName("CarryServiceLevelPrice")]
        public Dictionary<int, MinMax<int>> CarryServiceLevelPrice { get; set; } = new()
        {
            { 1, new () { Min = 24000, Max = 24000} },
            { 2, new () { Min = 25500, Max = 25500} },
            { 3, new () { Min = 27000, Max = 27000} },
            { 4, new () { Min = 28500, Max = 28500} },
            { 5, new () { Min = 30000, Max = 30000} }
        };
    }

    public record McsPluginConfig
    {
        [JsonPropertyName("Client")]
        public required McsPluginClientConfig ClientConfig { get; set; }

        [JsonPropertyName("Server")]
        public required McsPluginServerConfig ServerConfig { get; set; }
    }

    public record OrderConfig
    {
        [JsonPropertyName("orderQuests")]
        public required List<RepeatableQuestConfig> OrderQuests { get; set; }
    }
}