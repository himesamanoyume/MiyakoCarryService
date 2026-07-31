
using EFT;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Events
{
    public class OnPlayerDeadEvent : IMcsEvent
    {
        public Player DeadPlayer { get; set; }
    }
}