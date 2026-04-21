
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class Compensation
    {
        [DataMember(Name = "McsLeadPlayerId")]
        public MongoID McsLeadPlayerId;
    }
}