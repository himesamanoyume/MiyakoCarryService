
using System.Reflection;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 原版中邀请玩家入队后不会选择对应的分类图标
    /// </summary>
    internal sealed class GroupPlayerViewModelClassConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GroupPlayerViewModelClass).GetConstructors()[0];

        [PatchPostfix]
        public static void Postfix(GroupPlayerViewModelClass __instance, GroupPlayerDataClass player)
        {
            __instance.Info.SelectedMemberCategory = __instance.Info.MemberCategory;
        }
    }
}