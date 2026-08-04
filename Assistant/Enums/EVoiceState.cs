namespace MiyakoCarryService.Assistant.Enums
{
    /// <summary>
    /// Assistant 内部语音管线当前所在的状态。
    /// </summary>
    public enum EVoiceState
    {
        Idle,
        Capturing,
        Transcribing,
        Interpreting,
        Dispatching,
    }
}