
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 原版中邀请玩家入队后不会选择对应的分类图标
    /// </summary>
    internal sealed class GroupPlayerViewModelClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GroupPlayerViewModelClass), nameof(GroupPlayerViewModelClass.UpdateFromAnotherItem), [typeof(GroupPlayerDataClass)]);

        [PatchPostfix]
        public static void Postfix(GroupPlayerDataClass other)
        {
            other.Info.SelectedMemberCategory = other.Info.MemberCategory;
        }
    }
}