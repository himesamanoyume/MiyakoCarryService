
using EFT;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Events
{
    public class McsBotPlayerActivatedEvent : IMcsEvent
    {
        public MongoID McsBotPlayerId { get; set; }
    }
}