
using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class McsBotPlayerType
    {
        [DataMember(Name = "Side")]
        public ESideType Side;
    }
}