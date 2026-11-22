
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Events
{
    public class McsBotPlayerActivatedEvent : IMcsEvent
    {
        public string McsBotPlayerId { get; set; }
    }
}