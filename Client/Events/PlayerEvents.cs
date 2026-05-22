using EFT;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Events
{
    public sealed class OnMcsLeadPlayerDownEvent : IMcsEvent
    {
        public MongoID McsLeadPlayerId { get; set; }
        public MongoID McsBotPlayerId { get; set; }
    }
}