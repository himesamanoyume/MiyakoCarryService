
using System.Collections.Generic;
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
        public bool EnableLooting = MiyakoCarryServicePlugin.EnableLooting.Value;

        [DataMember(Name = "PriceThreshold")]
        public int PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value;

        [DataMember(Name = "KeywordItemText")]
        public string KeywordItemText = MiyakoCarryServicePlugin.KeywordItemText.Value;

        [DataMember(Name = "LootingKeywordItem")]
        public bool LootingKeywordItem = MiyakoCarryServicePlugin.LootingKeywordItem.Value;

        [DataMember(Name = "BlockItemType")]
        public int BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value;

        [DataMember(Name = "FormationMatrix")]
        public string FormationMatrix = MiyakoCarryServicePlugin.FormationMatrix.Value;

        [DataMember(Name = "EnableKeepFormation")]
        public bool EnableKeepFormation = MiyakoCarryServicePlugin.EnableKeepFormation.Value;

        [DataMember(Name = "FormationSpacing")]
        public float FormationSpacing = MiyakoCarryServicePlugin.FormationSpacing.Value;

        [DataMember(Name = "SequentialFill")]
        public bool SequentialFill = MiyakoCarryServicePlugin.SequentialFill.Value;

        [DataMember(Name = "Extensions")]
        public Dictionary<string, McsValue> Extensions = new();
    }
}