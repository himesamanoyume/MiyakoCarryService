
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
        public double Diff
        {
            get;
            set
            {
                field += value;
                if (field < 0)
                {
                    field = 0;
                }
            }
        }

        [DataMember(Name = "TeamKill")]
        public bool TeamKill = false;
    }
}