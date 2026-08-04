namespace MiyakoCarryService.Assistant.Enums
{
    /// <summary>
    /// 语音触发方式。<c>PushToTalk</c> 按住键录音松开处理；<c>FreeTalk</c> 通过 VAD 自动起止录音。无按键切换录音模式。
    /// </summary>
    public enum EVoiceTriggerMode
    {
        PushToTalk,
        FreeTalk,
    }
}