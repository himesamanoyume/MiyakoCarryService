
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsCommonLayer : McsBaseLayer
    {
        public McsCommonLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override void Start()
        {
            base.Start();
            _nextLootingCheckTime = Time.time + ENTER_COMMON_LOOTING_COLDDOWN;
        }

        public override Action GetNextAction()
        {
            try
            {
                var time = Time.time;
                if (McsBotPlayerData == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:DataNull");
                }

                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
                if (mcsLeadPlayerPos == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:LeadPosNull");
                }

                if (McsBotPlayerData.HasDecision(Decisions.ShouldDropTargetLoot) && BotOwner.ExternalItemsController.HaveItemsToDrop())
                {
                    if (_nextUpdatePosTime < time)
                    {
                        UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                        _nextUpdatePosTime = time + nextTime;
                    }

                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                        return new Action(typeof(DropTargetLootLogic), "Mcs:DropTargetLootCommand");
                    }
                    else
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:GoToPointCommandTargetPosNotFound");
                    }
                }

                if (McsBotPlayerData.HasDecision(Decisions.ShouldGoToPoint))
                {
                    if (_nextUpdatePosTime < time)
                    {
                        UpdateCommonMoveTarget(McsBotPlayerData.TargetPos, out float nextTime);
                        _nextUpdatePosTime = time + nextTime;
                    }

                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                        return new Action(typeof(GoToPointLogic), "Mcs:GoToPointCommand");
                    }
                    else
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:GoToPointCommandTargetPosNotFound");
                    }
                }

                if (McsBotPlayerData.HasDecision(Decisions.ShouldHoldPosition))
                {
                    if ((BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use))
                    {
                        RefreshStuckTimer();
                        return new Action(typeof(HealLogic), "Mcs:CommonHealing1");
                    }

                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionCommand");
                }

                if (BotOwner.Medecine.Stimulators.HaveSmt && Time.time > _nextStimCheckTime)
                {
                    _nextStimCheckTime = Time.time + 30f;
                    return new Action(typeof(HealStimulatorsLogic), "Mcs:UseStim");
                }

                if (!CheckFirearmsAnimatorState())
                {
                    BotOwner.TryResetHandsState();
                }

                CheckWeaponSwitch();

                if ((BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use))
                {
                    if (_nextUpdatePosTime < time)
                    {
                        UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                        _nextUpdatePosTime = time + nextTime;
                    }
                    
                    RefreshStuckTimer();
                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                    }
                    return new Action(typeof(HealLogic), "Mcs:CommonHealing2");
                }
                else if (TryGetBtrFollowAction(time, out var btrAction))
                {
                    return btrAction;
                }
                else if (_nextLootingCheckTime < time && McsBotPlayerData.McsAILeadPlayer.McsBotPlayerConfig.EnableLooting && McsBotPlayerData.LootingTarget != null && !McsBotPlayerData.HasDecision(Decisions.ShouldRegroup))
                {
                    if (_nextUpdatePosTime < time)
                    {
                        UpdateCommonMoveTarget(McsBotPlayerData.LootingTarget.RootTransform.position, out float nextTime);
                        _nextUpdatePosTime = time + nextTime;
                    }

                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                        return new Action(typeof(GoToLootTargetLogic), "Mcs:GoToLootTarget");
                    }
                    else
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:GoToLootTargetPosNotFound");
                    }
                }

                if (_nextUpdatePosTime < time)
                {
                    UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                    _nextUpdatePosTime = time + nextTime;
                }

                var sqrDistance = BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos);
                var tooClose = sqrDistance <= TOO_CLOSE_FROM_LEAD_DISTANCE * TOO_CLOSE_FROM_LEAD_DISTANCE;
                if (sqrDistance >= TOO_FAR_FROM_LEAD_DISTANCE * 1 || tooClose)
                {
                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                        return new Action(typeof(GoToPointLogic), tooClose ? "Mcs:TooClose" : "Mcs:TooFar");
                    }

                    return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindPath1");
                }
                else
                {
                    if (_nextPatrolTime < time)
                    {
                        _nextPatrolTime = time + 8f;
                        if (_currentMoveTarget.HasValue)
                        {
                            BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                            return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                        }
                        return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindPath2");
                    }
                    else
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:HoldPosition");
                    }
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(HoldPositionLogic), "Mcs:Exception");
            }
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

            if (BotOwner.Memory.IsUnderFire)
            {
                return false;
            }

            if (BotOwner.Memory.HaveEnemy && MiyakoCarryServicePlugin.SAINInstalled)
            {
                return false;
            }

            if (McsBotPlayerData != null && McsBotPlayerData.HasDecision(Decisions.ShouldTeleport))
            {
                McsBotPlayerData.RemoveDecision(Decisions.ShouldTeleport);
                UpdateLeadNearMoveTarget(McsBotPlayerData.LeadPlayer.Position, out float nextTime);
                if (_currentMoveTarget.HasValue)
                {
                    BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                }
            }

            return true;
        }
    }
}