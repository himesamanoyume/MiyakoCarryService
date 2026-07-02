using EFT;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Events
{
    public class GameWorldStartedEvent : IMcsEvent
    {
        public GameWorld GameWorld { get; set; }
    }

    public class GameWorldEndedEvent : IMcsEvent
    {
        public ExitStatus ExitStatus { get; set; }
    }
}