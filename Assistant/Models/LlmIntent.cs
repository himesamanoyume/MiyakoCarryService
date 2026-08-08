using System.Collections.Generic;
using MiyakoCarryService.Assistant.Enums;

namespace MiyakoCarryService.Assistant.Models
{
    public sealed class LlmIntent
    {
        public const string NotRecognized = "not_recognized";
        public string CommandName;
        public EIntentTargetSelector Selector = EIntentTargetSelector.Unspecified;
        public int? TargetIndex;
        public string TargetCodeName;
        public List<int> TargetIndices;
        public List<string> TargetCodeNames;
        public string AimingBodyPart;
        public int? OptionIndex;
        public string ReplyText;
        public string Error;
        public bool IsReply => !string.IsNullOrEmpty(ReplyText);
        public bool IsError => !string.IsNullOrEmpty(Error);
    }
}