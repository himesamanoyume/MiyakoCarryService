
using System.Runtime.Serialization;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class Compensation
    {
        [DataMember(Name = "McsLeadPlayerId")]
        public string McsLeadPlayerId;
    }
}