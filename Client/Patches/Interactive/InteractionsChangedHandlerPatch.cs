using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Interactive
{
    /// <summary>
    /// 指令菜单的 4.1 语义（事件驱动，而非每帧轮询）：
    /// 游戏侧唯一的状态生产点是 <c>GamePlayerOwner.InteractionsChangedHandler</c>
    /// （4.1 源码 GamePlayerOwner.cs:614 → :645 `AvailableInteractionState.Value = availableInteractionState;`）。
    /// </summary>
    /// <remarks>
    /// ⚠️ 为什么不去 patch 那个 setter：<c>BindableState&lt;T&gt;.set_Value</c> 是 IL2CPP **泛型共享**方法，
    /// Harmony 打完补丁后调用“原方法”会走 <c>il2cpp_runtime_invoke</c>，而该路径缺泛型上下文 ⇒ 进程级 AV
    ///（实测 Fatal AccessViolationException，栈顶就是 il2cpp_runtime_invoke）。
    /// 因此改为在本方法的 **postfix** 里事后纠正：菜单打开时把游戏刚产出的动作并入我们的菜单，再把状态换回我们的菜单。
    /// 本方法是非泛型（protected virtual），补丁安全。
    /// </remarks>
    public sealed class InteractionsChangedHandlerPatch : ModulePatch
    {
        /// <summary>
        /// 补丁是否成功启用（供 CommandUtils 判断兜底路径的必要性）。
        /// </summary>
        public static bool IsActive { get; private set; }

        protected override MethodBase GetTargetMethod()
        {
            MethodBase target = AccessTools.Method(typeof(GamePlayerOwner), "InteractionsChangedHandler");

            IsActive = target != null;

            return target;
        }

        [PatchPostfix]
        public static void Postfix(GamePlayerOwner __instance)
        {
            // 不吞异常：出错就让 Il2CppInterop 的 trampoline 把真实调用栈打到日志里
            CommandUtils.OnGameProducedInteractionState(__instance);
        }
    }
}
