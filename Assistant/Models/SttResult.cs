namespace MiyakoCarryService.Assistant.Models
{
    public sealed class SttResult
    {
        public string Text;
        public string DetectedLanguage;
        public string Error;
        public bool IsSuccess => string.IsNullOrEmpty(Error);
    }
}