using System.Collections.Generic;
using MiyakoCarryService.Assistant.Enums;

namespace MiyakoCarryService.Assistant.Models
{
    /// <summary>
    /// LLM 解析后产生的语音指令意图，可作为目标用例的字段：
    /// <list type="bullet">
    ///   <item><c>CommandName</c> 是 <see cref="MiyakoCarryService.Client.Enums.ECommandType"/> 的枚举名字（如 <c>FollowMe</c>）。</item>
    ///   <item><c>Selector</c>=<see cref="EIntentTargetSelector.All"/> 时对全员执行。</item>
    ///   <item><c>Selector</c>=<see cref="EIntentTargetSelector.ByIndex"/> 时使用 <c>TargetIndex</c> 选择 1-based 序号的护航成员。</item>
    ///   <item><c>Selector</c>=<see cref="EIntentTargetSelector.ByCodeName"/> 时使用 <c>TargetCodeName</c> 选择匹配的护航代号。</item>
    ///   <item><c>ReplyText</c> 非空时为纯聊天回复。</item>
    ///   <item><c>AimingBodyPart</c> 仅用于 <c>AimingBodyPart</c> 指令。</item>
    /// </list>
    /// </summary>
    public sealed class LlmIntent
    {
        /// <summary>LLM 未识别出任何指令时的统一错误标记（提示词规定返回该值）。</summary>
        public const string NotRecognized = "not_recognized";

        public string CommandName;
        public EIntentTargetSelector Selector = EIntentTargetSelector.Unspecified;
        public int? TargetIndex;
        public string TargetCodeName;
        // 多目标：一次指定多名护航（如 "5号6号" / "Rabbit1、Rabbit2"），优先于单目标字段
        public List<int> TargetIndices;
        public List<string> TargetCodeNames;
        public string AimingBodyPart;
        // 代理/护送类指令：玩家话术命中的"指令菜单选项"序号（1-based，对应提示词中注入的编号列表）
        public int? OptionIndex;
        public string ReplyText;
        public string Error;

        public bool IsReply => !string.IsNullOrEmpty(ReplyText);
        public bool IsError => !string.IsNullOrEmpty(Error);
    }
}