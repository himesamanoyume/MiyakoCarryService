using System.Collections.Generic;
using Newtonsoft.Json;

namespace MiyakoCarryService.Assistant.Models.Providers
{
    public sealed class LlmIntentJson
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("selector")]
        public string Selector { get; set; }

        [JsonProperty("targetIndex")]
        public string TargetIndex { get; set; }

        [JsonProperty("targetIndices")]
        public List<string> TargetIndices { get; set; }

        [JsonProperty("targetCodeName")]
        public string TargetCodeName { get; set; }

        [JsonProperty("targetCodeNames")]
        public List<string> TargetCodeNames { get; set; }

        [JsonProperty("aimingBodyPart")]
        public string AimingBodyPart { get; set; }

        [JsonProperty("optionIndex")]
        public string OptionIndex { get; set; }

        [JsonProperty("replyText")]
        public string ReplyText { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }
}