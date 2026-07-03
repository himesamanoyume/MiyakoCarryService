
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class McsBotPlayerConfig
    {
        [DataMember(Name = "McsLeadPlayerId")]
        public MongoID McsLeadPlayerId = GameLoop.Instance.Session.Profile.Id;

        [DataMember(Name = "EnableLooting")]
        public bool EnableLooting = MiyakoCarryServicePlugin.EnableLooting.Value;

        [DataMember(Name = "PriceThreshold")]
        public int PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value;

        [DataMember(Name = "KeywordItemText")]
        public string KeywordItemText = MiyakoCarryServicePlugin.KeywordItemText.Value;

        [DataMember(Name = "LootingKeywordItem")]
        public bool LootingKeywordItem = MiyakoCarryServicePlugin.LootingKeywordItem.Value;

        [DataMember(Name = "BlockItemType")]
        public int BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value;
    }
}