using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Interactive
{
    /// <summary>
    /// 与 <see cref="InteractionsChangedHandlerPatch"/> 配套：游戏调用 <c>GamePlayerOwner.ClearInteractionState()</c>
    /// （4.1 源码 GamePlayerOwner.cs:648-651，内部 `AvailableInteractionState.Value = null;`）会清空状态，
    /// 我们只应在自己主动关闭指令菜单时允许它被清空，因此在 postfix 里把状态重新挂回我们的菜单。
    /// </summary>
    /// <remarks>
    /// 同前：不去 patch 泛型 setter（会 AV），本方法非泛型，补丁安全。
    /// </remarks>
    public sealed class ClearInteractionStatePatch : ModulePatch
    {
        /// <summary>
        /// 补丁是否成功启用（供 CommandUtils 判断兜底路径的必要性）。
        /// </summary>
        public static bool IsActive { get; private set; }

        protected override MethodBase GetTargetMethod()
        {
            MethodBase target = AccessTools.Method(typeof(GamePlayerOwner), "ClearInteractionState");

            IsActive = target != null;

            return target;
        }

        [PatchPostfix]
        public static void Postfix(GamePlayerOwner __instance)
        {
            // 不吞异常：出错就让 Il2CppInterop 的 trampoline 把真实调用栈打到日志里
            CommandUtils.ReassertCommandMenu(__instance);
        }
    }
}
