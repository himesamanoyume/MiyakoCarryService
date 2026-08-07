using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Assistant.Services
{
    /// <summary>
    /// 对玩家准星射线做物理投射，用作 <c>ECommandType.GoToPoint</c>/<c>EscortWorld</c>/<c>QuestProxyAction</c>
    /// 等"需要位置/目标"类语音指令的隐式目标。复刻 MCS 菜单流程中 Resolver 的取目标逻辑。
    /// </summary>
    internal static class TargetResolver
    {
        public sealed class ResolvedTarget
        {
            public Vector3? Position;
            public string TargetId;
        }

        /// <summary>
        /// 取玩家准星在世界中的目标。命中 → 命中点 + (可能的)目标 Id；未命中 → 前方 maxDistance 处空地。
        /// <c>TargetId</c> 只有在命中 loot/quest 互动物时由 LLM 直接 verbal 化指定时使用；
        /// 实际的 id-to-WorldInteractiveObject 解析由 McsCommandApi 自身的 Resolver 在调用 ProcessCommand 时完成。
        /// </summary>
        public static ResolvedTarget ResolveForPlayer(Player player, float maxDistance = 1000f)
        {
            if (player == null) { return null; }

            // 复刻 MCS 菜单流程的 Resolver：直接用玩家 InteractionRay 投射，命中即取 hit.point。
            if (Physics.Raycast(player.InteractionRay, out var hit, maxDistance, LayersMaskController.HighPolyWithTerrainMask))
            {
                return new ResolvedTarget { Position = hit.point, TargetId = null };
            }

            var fwdPos = player.Position + player.InteractionRay.direction * Mathf.Min(maxDistance, 64f);
            return new ResolvedTarget { Position = fwdPos, TargetId = null };
        }

        /// <summary>
        /// 代理开门/代理拾取战利品的"选项显示状态"判定（复刻 GetActionsClassPatches 的条件）：
        /// <c>InteractionProxyAction</c> 要求准星命中 <see cref="EDoorState.Locked"/> 的门；
        /// <c>LootProxyAction</c> 要求命中非尸体的 LootItem。
        /// 条件不满足时返回 null（调用方应提示"请对准目标"）。
        /// </summary>
        public static ResolvedTarget ResolveProxyTarget(Player player, string commandType, float maxDistance = 1000f)
        {
            if (player == null)
            {
                return null;
            }

            if (!Physics.Raycast(player.InteractionRay, out var hit, maxDistance, LayersMaskController.HighPolyWithTerrainMask))
            {
                return null;
            }

            if (commandType == ECommandType.InteractionProxyAction.ToString())
            {
                var door = hit.collider.GetComponentInParent<Door>();
                if (door != null && door.DoorState == EDoorState.Locked)
                {
                    var doorData = door.GetData();
                    return doorData != null
                        ? new ResolvedTarget { Position = hit.point, TargetId = doorData.Id() }
                        : null;
                }
                return null;
            }

            if (commandType == ECommandType.LootProxyAction.ToString())
            {
                var lootItem = hit.collider.GetComponentInParent<LootItem>();
                if (lootItem != null && !(lootItem is Corpse))
                {
                    if (lootItem.Item.GetData() is LootData lootData)
                    {
                        return new ResolvedTarget { Position = hit.point, TargetId = lootData.Item.Id };
                    }
                }
                return null;
            }

            // 其他代理类：退化为命中点（无目标 Id）
            return new ResolvedTarget { Position = hit.point, TargetId = null };
        }
    }
}
