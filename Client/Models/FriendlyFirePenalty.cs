
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class FriendlyFirePenalty
    {
        [DataMember(Name = "FriendlyFireLeadPlayerId")]
        public MongoID FriendlyFireLeadPlayerId;

        [DataMember(Name = "Diff")]
        public double Diff;

        [DataMember(Name = "TeamKill")]
        public bool TeamKill = false;
    }
}