using UnityEngine;

namespace MiyakoCarryService.Client.Models
{
    /// <summary>
    /// 供语音管线枚举的指令菜单选项（代理/护送类），与玩家手动打开的子菜单选项一一对应，
    /// 名称即本地化显示名（含距离提示）。选项顺序在单局内稳定，可安全用 1-based 序号引用。
    /// </summary>
    public sealed class VoiceMenuOption
    {
        public string Name;
        public string TargetName;
        public string CommandType;
        public Vector3? Position;
        public string TargetId;
        public bool Disabled;
    }
}
