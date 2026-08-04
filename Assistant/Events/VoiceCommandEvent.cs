using MiyakoCarryService.Assistant.Enums;
using MiyakoCarryService.Assistant.Models;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Assistant.Events
{
    /// <summary>
    /// 一次完整语音指令管线的最终事件，可用于通知 SubtitlesMgr 显示反馈字幕等下游消费者。
    /// 通过 <c>McsEventApi.Notify</c> 触发；订阅者可通过 <c>McsEventApi.Subscribe&lt;VoiceCommandEvent&gt;</c> 监听。
    /// </summary>
    public class VoiceCommandEvent : IMcsEvent
    {
        /// <summary>识别出的原始转写文本（可能为空）。</summary>
        public string TranscribedText;

        /// <summary>LLM 返回的指令意图。<c>null</c> 表示 LLM 未启用或本路属于纯回复。</summary>
        public LlmIntent Intent;

        /// <summary>本次管线最终结局。</summary>
        public EVoiceState State;

        /// <summary>本次实际执行的护航成员数量（0 表示未派发）。</summary>
        public int DispatchedMembers;

        /// <summary>对玩家本地的简短反馈文案（可显示为字幕/通知）。</summary>
        public string FeedbackMessage;
    }
}