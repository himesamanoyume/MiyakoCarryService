
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsCommonLayer : McsBaseLayer<McsCommonLayer>
    {
        public McsCommonLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override void Start()
        {
            base.Start();
            _nextLootingCheckTime = Time.time + ENTER_COMMON_LOOTING_COLDDOWN;
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override Action GetNextAction()
        {
            try
            {
                if (McsBotPlayerData != null)
                {
                    if (McsBotPlayerData.HasDecision(EDecision.ShouldGoToPoint))
                    {
                        return new Action(typeof(GoToPointLogic), "Mcs:GoToPointCommand");
                    }

                    if (McsBotPlayerData.HasDecision(EDecision.ShouldHoldPosition))
                    {
                        if (BotOwner.Medecine.FirstAid.Damaged)
                        {
                            if (BotOwner.Medecine.FirstAid.HaveSmth2Use)
                            {
                                if (!BotOwner.Memory.IsInCover)
                                {
                                    return new Action(typeof(RunToCoverLogic), "Mcs:RunToCoverForFirstAid");
                                }
                            }
                        }

                        if (BotOwner.Medecine.SurgicalKit.Damaged)
                        {
                            if (BotOwner.Medecine.SurgicalKit.HaveSmth2Use)
                            {
                                if (!BotOwner.Memory.IsInCover)
                                {
                                    return new Action(typeof(RunToCoverLogic), "Mcs:RunToCoverForSurgical");
                                }
                            }
                        }

                        if ((BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use))
                        {
                            return new Action(typeof(HealLogic), "Mcs:Healing");
                        }

                        return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionCommand");
                    }
                }

                if (BotOwner.Medecine.FirstAid.Damaged)
                {
                    if (BotOwner.Medecine.FirstAid.HaveSmth2Use)
                    {
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:RunToCoverForFirstAid");
                        }
                    }
                }

                if (BotOwner.Medecine.SurgicalKit.Damaged)
                {
                    if (BotOwner.Medecine.SurgicalKit.HaveSmth2Use)
                    {
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:RunToCoverForSurgicalKit");
                        }
                    }
                }

                if ((BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use))
                {
                    return new Action(typeof(HealLogic), "Mcs:Healing");
                }

                CheckWeaponSwitch();

                CheckDanger(true);

                if (BotOwner.BewarePlantedMine.CanDeactivate())
                {
                    return new Action(typeof(DeactivateMineLogic), "Mcs:DeactivateMine");
                }

                if (McsBotPlayerData != null)
                {
                    // 检测周围是否有符合条件的战利品
                    if (McsBotPlayerData.McsAILeadPlayer.McsBotPlayerConfig.EnableLooting && McsBotPlayerData.LootingTarget != null && _nextLootingCheckTime < Time.time && !McsBotPlayerData.HasDecision(EDecision.ShouldRegroup))
                    {
                        // 尝试去拿战利品
                        return new Action(typeof(GoToLootTargetLogic), "Mcs:GoToLootTarget");
                    }
                }

                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
                if (mcsLeadPlayerPos == null)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:leadPosNull");
                }

                if (_nextUpdatePosTime < Time.time)
                {
                    UpdateMoveTarget(out float nextTime);
                    _nextUpdatePosTime = Time.time + nextTime;
                }

                var sqrDistance = BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos);
                var tooClose = sqrDistance <= TOO_CLOSE_FROM_LEAD_DISTANCE * TOO_CLOSE_FROM_LEAD_DISTANCE;
                if (sqrDistance >= TOO_FAR_FROM_LEAD_DISTANCE * 1 || tooClose)
                {
                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                        return new Action(typeof(GoToPointLogic), tooClose ? "Mcs:TooClose" : "TooFar");
                    }

                    return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindPath1");
                }
                else
                {
                    if (_nextPatrolTime < Time.time)
                    {
                        _nextPatrolTime = Time.time + 8f;
                        var validPosition = Tools.GetPosNearTarget(mcsLeadPlayerPos);
                        if (validPosition.HasValue)
                        {
                            BotOwner.GoToSomePointData.SetPoint(validPosition.Value);
                            return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                        }

                        return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindPath2");
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
                return new Action(typeof(SimplePatrolLogic), "Mcs:Exception");
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

            return true;
        }
    }
}