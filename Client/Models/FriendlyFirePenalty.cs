
using System.Runtime.Serialization;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class FriendlyFirePenalty
    {
        [DataMember(Name = "FriendlyFirePlayerId")]
        public string FriendlyFirePlayerId;

        [DataMember(Name = "Diff")]
        public double Diff;

        [DataMember(Name = "TeamKill")]
        public bool TeamKill = false;

        [DataMember(Name = "PunishEveryone")]
        public bool PunishEveryone = false;
    }
}