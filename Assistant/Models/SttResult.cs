namespace MiyakoCarryService.Assistant.Models
{
    public class SttResult
    {
        public string Text;
        public string DetectedLanguage;
        public string Error;
        public bool IsSuccess => string.IsNullOrEmpty(Error);
    }
}