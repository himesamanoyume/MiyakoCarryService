
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class McsBotPlayerConfig
    {
        [DataMember(Name = "McsLeadPlayerId")]
        public MongoID McsLeadPlayerId;

        [DataMember(Name = "EnableLooting")]
        public bool EnableLooting = false;

        [DataMember(Name = "PriceThreshold")]
        public int PriceThreshold = 50000;

        [DataMember(Name = "KeywordItemText")]
        public string KeywordItemText = "";

        [DataMember(Name = "LootingKeywordItem")]
        public bool LootingKeywordItem = false;

        [DataMember(Name = "BlockItemType")]
        public int BlockItemType = 0;
    }
}