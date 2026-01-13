using System.Runtime.Serialization;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public struct SpawnCarryServiceBotRequest
    {
        [DataMember(Name = "hostSessionId")]
        public MongoID HostSessionId;

        public SpawnCarryServiceBotRequest(MongoID hostSessionId)
        {
            HostSessionId = hostSessionId;
        }
    }
}