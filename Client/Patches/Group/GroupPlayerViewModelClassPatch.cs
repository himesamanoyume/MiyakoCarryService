
using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 原版中邀请玩家入队后不会选择对应的分类图标
    /// </summary>
    public sealed class GroupPlayerViewModelClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RaidPlayer), nameof(RaidPlayer.UpdateFromAnotherItem), [typeof(GroupPlayer)]);

        [PatchPostfix]
        public static void Postfix(GroupPlayer other)
        {
            other.Info.SelectedMemberCategory = other.Info.MemberCategory;
        }
    }
}