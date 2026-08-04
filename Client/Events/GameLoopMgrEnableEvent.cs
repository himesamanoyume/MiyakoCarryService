
using System;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Events
{
    public class GameLoopMgrEnableEvent : IMcsEvent
    {
        public Type[] MgrTypes { get; set; }
    }
}