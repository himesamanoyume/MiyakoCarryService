using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsClearAreaLayer : McsBaseLayer
    {
        private const float ARRIVE_DIST = 2.5f;
        private const float LOOK_AROUND_TIME = 1.5f;
        private const float STUCK_TIMEOUT = 8f;

        public McsClearAreaLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                BotOwner.TalkMsg(new McsMsg { PhraseTrigger = EPhraseTrigger.Going });
            }
        }

        public override Action GetNextAction()
        {
            try
            {
                var time = Time.time;

                if (McsBotPlayerData == null || McsBotPlayerData.ClearAreaPoints == null || McsBotPlayerData.ClearAreaPoints.Count == 0)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:ClearAreaNoPoints");
                }

                if (McsBotPlayerData.ClearAreaIndex >= McsBotPlayerData.ClearAreaPoints.Count)
                {
                    FinishClearArea();
                    return new Action(typeof(SimplePatrolLogic), "Mcs:ClearAreaDone");
                }

                var targetPos = McsBotPlayerData.ClearAreaPoints[McsBotPlayerData.ClearAreaIndex];
                McsBotPlayerData.TargetPos = targetPos;

                var arrived = BotOwner.Position.McsSqrDistance(targetPos) <= ARRIVE_DIST * ARRIVE_DIST;
                var stuck = BotOwner.Mover.LastTimePosChanged + STUCK_TIMEOUT < time;

                if (arrived || stuck)
                {
                    if (GClass856.IsTrue100(25f) && arrived)
                    {
                        if (McsBotPlayerData.ClearAreaLookAroundUntil <= 0f)
                        {
                            McsBotPlayerData.ClearAreaLookAroundUntil = time + LOOK_AROUND_TIME;
                            BotOwner.StopMove();
                        }

                        if (time < McsBotPlayerData.ClearAreaLookAroundUntil)
                        {
                            var yaw = time * 90f % 360f;
                            var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                            BotOwner.Steering.LookToDirection(dir, 120f);
                            return new Action(typeof(HoldPositionLogic), "Mcs:ClearAreaLookAround");
                        }
                    }

                    McsBotPlayerData.ClearAreaLookAroundUntil = 0f;
                    McsBotPlayerData.ClearAreaIndex++;
                    BotOwner.Mover.LastTimePosChanged = time;

                    if (McsBotPlayerData.ClearAreaIndex >= McsBotPlayerData.ClearAreaPoints.Count)
                    {
                        FinishClearArea();
                        return new Action(typeof(SimplePatrolLogic), "Mcs:ClearAreaDone");
                    }

                    targetPos = McsBotPlayerData.ClearAreaPoints[McsBotPlayerData.ClearAreaIndex];
                    McsBotPlayerData.TargetPos = targetPos;
                }

                if (_nextUpdatePosTime < time)
                {
                    UpdateCommonMoveTarget(McsBotPlayerData.TargetPos, out float nextTime);
                    _nextUpdatePosTime = time + nextTime;
                }

                if (_currentMoveTarget.HasValue)
                {
                    BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                    return new Action(typeof(GoToPointLogic), "Mcs:ClearAreaGoToPoint");
                }

                return new Action(typeof(SimplePatrolLogic), "Mcs:ClearAreaCannotFindPath");
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(SimplePatrolLogic), "Mcs:Exception");
            }
        }

        private void FinishClearArea()
        {
            McsBotPlayerData.ClearAreaPoints = null;
            McsBotPlayerData.ClearAreaIndex = 0;
            McsBotPlayerData.ClearAreaLookAroundUntil = 0f;
            McsBotPlayerData.TargetPos = null;
            McsBotPlayerData.RemoveDecision(Decisions.ShouldClearArea);
            BotOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Clear
            });
        }

        public override bool IsActive()
        {
            if (!IsMcsBotPlayer)
            {
                return false;
            }

#if DEBUG
            if (!MiyakoCarryServicePlugin.EnableMcsLayer.Value)
            {
                return false;
            }
#endif

            if (McsBotPlayerData == null)
            {
                return false;
            }

            if (BotOwner.Memory.IsUnderFire)
            {
                return false;
            }

            if (!McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
            {
                return false;
            }

            if (McsBotPlayerData.HasDecision(Decisions.ShouldClearArea) && McsBotPlayerData.ClearAreaPoints != null && McsBotPlayerData.ClearAreaPoints.Count > 0)
            {
                return true;
            }

            return false;
        }
    }
}