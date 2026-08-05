using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Assistant.Events
{
    /// <summary>
    /// 一次完整语音指令管线的最终事件，可用于通知 SubtitlesMgr 显示反馈字幕等下游消费者
    /// </summary>
    public class VoiceCommandEvent : IMcsEvent
    {
        public string TranscribedText;
        public LlmIntent Intent;
        public EVoiceState State;
        public int DispatchedMembers;
        public string FeedbackMessage;
    }
}