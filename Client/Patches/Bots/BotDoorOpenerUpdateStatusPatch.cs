using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    public sealed class BotDoorOpenerUpdateStatusPatch : ModulePatch
    {
        /// <summary>
        /// 门区半径平方（米²）：√27.04 ≈ 5.2m，原生 CheckCanRun 停冲刺判定同款
        /// </summary>
        private const float DOOR_ZONE_SPRINT_SQR = 27.04f;

        /// <summary>
        /// 门区方向锥最小点积：移动方向与朝门方向夹角约 53° 以内才算"冲着门去"（过滤路过的侧向门）
        /// </summary>
        private const float DOOR_ZONE_DIR_DOT = 0.6f;

        /// <summary>
        /// 门区冲刺暂停时长（秒）：短续期（离开门区 0.5s 内自动恢复冲刺，跟随 Logic 每 tick 的
        /// Sprint(true) 在 NoSprint 过期后自然生效）
        /// </summary>
        private const float DOOR_ZONE_SPRINT_PAUSE = 0.5f;

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.UpdateDoorInteractionStatus));

        [PatchPrefix]
        public static bool Prefix(BotDoorOpener __instance, ref DoorInteractionStatus __result)
        {
            var botOwner = __instance._owner;
            if (botOwner == null || !botOwner.IsMcsBotPlayer)
            {
                return true;
            }

            // 已在原生门交互中（NearDoor 态/Interacting）不压制：让其自然走完/自愈退出，
            // 避免中途短路造成 mover 状态机悬挂
            if (__instance.Interacting || botOwner.Mover.CurrentState == EBotMoverState.NearDoor)
            {
                return true;
            }

            // 门区冲刺保护：前方有关闭/摆动中的门，或移动方向与敞开门叶相交 → OpeningDoor + 杀冲刺
            if (IsInDoorSprintZone(botOwner))
            {
                __result = DoorInteractionStatus.OpeningDoor;
                // 显式打断进行中冲刺（BotMover.Sprint 已冲刺时早退不查 NoSprint，光 SprintPause 无效）
                botOwner.Mover.Sprint(false);
                botOwner.Mover.SprintPause(DOOR_ZONE_SPRINT_PAUSE);
                return false;
            }

            // 无门区威胁：可自由冲刺，开门交给快速开门链路
            __result = DoorInteractionStatus.CanRun;
            return false;
        }

        /// <summary>
        /// 门区判定：移动前方 5.2m 内有关闭/摆动中的门（正对接近），或移动方向与敞开门的
        /// 门叶线（SegmentClose）相交（叶挡路）——两者都会因冲刺速度撞上静止/旋转中的门板
        /// 造成隧道穿透（冲刺每帧位移 8-10cm ≥ 门板厚度）
        /// </summary>
        private static bool IsInDoorSprintZone(BotOwner botOwner)
        {
            var doorLinks = botOwner.NearDoorData.CurrentDoorLinks();
            if (doorLinks.Count == 0)
            {
                return false;
            }

            var botPosition = botOwner.Position;
            var targetPoint = botOwner.Mover.TargetPoint;
            var moveDir = Vector3.zero;
            if (targetPoint.HasValue)
            {
                moveDir = targetPoint.Value - botPosition;
                moveDir.y = 0f;
                if (moveDir.sqrMagnitude < 0.01f)
                {
                    return false;
                }
                moveDir.Normalize();
            }

            for (var i = 0; i < doorLinks.Count; i++)
            {
                var doorLink = doorLinks[i];
                var door = doorLink?.Door;
                if (door == null)
                {
                    continue;
                }

                var doorState = door.DoorState;
                if (doorState == EDoorState.Open)
                {
                    // 敞开门：仅当移动方向与门叶线（打开后的门板位置）相交才限速
                    // （原生 WantPassTheDoor 的 wantClose 同款几何——叶挡路，冲刺斜切同样穿透）
                    if (targetPoint.HasValue)
                    {
                        var segmentClose = doorLink.SegmentClose;
                        if (AIUtility.GetCrossPoint(botPosition, targetPoint.Value, segmentClose.a, segmentClose.b) != null)
                        {
                            return true;
                        }
                    }
                    continue;
                }

                // 关闭/摆动中（Shut/Interacting/Breaching）：正对接近即限速
                var doorPos = doorState == EDoorState.Open ? doorLink.MidClose : doorLink.MidOpen;
                if (botPosition.McsSqrDistance(doorPos) > DOOR_ZONE_SPRINT_SQR)
                {
                    continue;
                }

                if (targetPoint.HasValue)
                {
                    var toDoor = doorPos - botPosition;
                    toDoor.y = 0f;
                    if (toDoor.sqrMagnitude < 0.01f || Vector3.Dot(moveDir, toDoor.normalized) < DOOR_ZONE_DIR_DOT)
                    {
                        continue;
                    }
                }

                return true;
            }

            return false;
        }
    }
}