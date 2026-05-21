
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class McsBotPlayerConfig
    {
        [DataMember(Name = "McsLeadPlayerId")]
        public MongoID McsLeadPlayerId;

        [DataMember(Name = "PriceThreshold")]
        public int PriceThreshold;

        [DataMember(Name = "KeywordItemText")]
        public string KeywordItemText;

        [DataMember(Name = "LootingKeywordItem")]
        public bool LootingKeywordItem;

        [DataMember(Name = "BlockItemType")]
        public int BlockItemType;
    }
}