
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class FriendlyFirePenalty
    {
        [DataMember(Name = "FriendlyFirePlayerId")]
        public MongoID FriendlyFirePlayerId;

        [DataMember(Name = "Diff")]
        public double Diff;

        [DataMember(Name = "TeamKill")]
        public bool TeamKill = false;

        [DataMember(Name = "PunishEveryone")]
        public bool PunishEveryone = false;
    }
}