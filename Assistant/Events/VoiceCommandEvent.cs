using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Assistant.Events
{
    public class VoiceCommandEvent : IMcsEvent
    {
        public string TranscribedText;
        public LlmIntent Intent;
        public EVoiceState State;
        public int DispatchedMembers;
        public string FeedbackMessage;
    }
}