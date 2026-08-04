namespace MiyakoCarryService.Assistant.Enums
{
    /// <summary>
    /// LLM 意图中识别出的护航成员选择方式。
    /// </summary>
    public enum EIntentTargetSelector
    {
        /// <summary>未指定，由 <see cref="MiyakoCarryService.Assistant.Services.IntentBinder"/> 决定默认（通常为全员）。</summary>
        Unspecified,
        /// <summary>对当前所有存活的护航成员执行。</summary>
        All,
        /// <summary>对按 1-based 索引指定的单个护航成员执行（<c>LlmIntent.TargetIndex</c>）。</summary>
        ByIndex,
        /// <summary>对按代号/昵称指定的单个护航成员执行（<c>LlmIntent.TargetCodeName</c>）。</summary>
        ByCodeName,
    }
}