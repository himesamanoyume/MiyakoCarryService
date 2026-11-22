using System.Reflection;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Interactive
{
    /// <summary>
    /// 指令菜单闪烁修复（4.1 语义的 UI 侧等价实现）。
    /// </summary>
    /// <remarks>
    /// 4.1 是在 <c>BindableState&lt;AvailableInteractionState&gt;.set_Value</c> 上打前缀，
    /// 在 <c>UpdateState</c> 触发事件之前就把游戏值换回我们的菜单，因此 UI 只会看到我们的菜单。
    /// 5.0 无法 patch 该 IL2CPP 泛型共享方法（调用原方法会走 il2cpp_runtime_invoke 缺泛型上下文 ⇒ AV），
    /// 改用 <c>InteractionsChangedHandler</c> 的 **postfix** 纠正：游戏值先进 bindable，
    /// <c>ActionPanel.AvailableInteractionStateChangedHandler</c> 先把交互按钮面板隐藏/清空，
    /// 随后 postfix 再挂回我们的菜单并重建 ⇒ 每次交互变化都「隐藏 → 显示」，表现为闪烁。
    /// 这里在 UI 的唯一订阅点上做前缀过滤：菜单打开期间，非本菜单的中间态一律不呈现，
    /// 即等价于 4.1 的 setter 拦截，同时保留 postfix 对世界物件动作的合并。
    /// </remarks>
    public sealed class ActionPanelInteractionStatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ActionPanel), "AvailableInteractionStateChangedHandler");

        [PatchPrefix]
        public static bool Prefix(AvailableInteractionState interactionState)
        {
            if (!CommandUtils.IsCommandMenuOpen)
            {
                return true;
            }

            // 菜单开着但游戏把状态清成 null：不要让它把面板隐藏掉（否则就是闪烁的那一下）。
            if (interactionState == null)
            {
                return false;
            }

            // ⚠️ 只放行"我们自己的菜单"，且必须用原生指针比较：
            // Harmony 传入的对象参数可能是同一原生对象的另一个托管包装，ReferenceEquals 会误判。
            // 以当前菜单指针为准，`_ourMenuPointers`（本会话创建过的菜单）作为兜底，
            // 保证自己的菜单永不被过滤，同时仍能挡掉游戏写回的中间态。
            return CommandUtils.IsCurrentMenu(interactionState) || CommandUtils.IsOurMenu(interactionState);
        }
    }
}
