
using System.Runtime.Serialization;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class McsBotPlayerAid
    {
        [DataMember(Name = "Aid")]
        public string Aid;
    }
}