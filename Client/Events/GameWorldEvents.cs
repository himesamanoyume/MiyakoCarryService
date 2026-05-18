using EFT;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Events
{
    public sealed class GameWorldStartedEvent : IMcsEvent
    {
        public GameWorld GameWorld { get; set; }
    }

    public sealed class GameWorldEndedEvent : IMcsEvent
    {
        public ExitStatus ExitStatus { get; set; }
    }
}