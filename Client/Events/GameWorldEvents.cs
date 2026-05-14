using System;
using EFT;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Events
{
    public sealed class GameWorldStartedEvent : IMcsEvent
    {
        public GameWorld GameWorld { get; set; }
        public DateTime StartTime { get; set; }
    }

    public sealed class GameWorldEndedEvent : IMcsEvent
    {
        public ExitStatus ExitStatus { get; set; }
        public DateTime EndTime { get; set; }
    }
}