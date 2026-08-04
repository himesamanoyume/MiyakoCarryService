namespace MiyakoCarryService.Assistant.Models
{
    /// <summary>
    /// STT 调用结果。<c>Error</c> 非空表示失败；<c>Text</c> 为识别后的自然语言文本。
    /// </summary>
    public sealed class SttResult
    {
        public string Text;

        public string DetectedLanguage;

        public string Error;

        public bool IsSuccess => string.IsNullOrEmpty(Error);
    }
}