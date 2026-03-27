
using System.Runtime.Serialization;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class DebugInfo
    {
        [DataMember(Name = "Info")]
        public string Info;
    }
}