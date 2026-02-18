
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class FriendlyFirePenalty
    {
        [DataMember(Name = "FriendlyFireLeadPlayerId")]
        public MongoID FriendlyFireLeadPlayerId;

        [DataMember(Name = "StandingDiff")]
        public double StandingDiff = 0;

        [DataMember(Name = "PunishEveryone")]
        public bool PunishEveryone = false;
    }
}