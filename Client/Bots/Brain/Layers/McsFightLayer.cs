
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsFightLayer : McsBaseLayer<McsFightLayer>
    {
        public McsFightLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.IsLooting = false;
            }

            if (BotOwner.Memory.GoalEnemy != BotOwner.Memory.LastEnemy)
            {
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.OnFirstContact,
                    Position = BotOwner.Memory.GoalEnemy.EnemyLastPosition
                });
            }
        }

        public override void Stop()
        {
            base.Stop();
            foreach (var member in BotOwner.BotsGroup.Members)
            {
                if (member.Memory.HaveEnemy)
                {
                    return;
                }
            }

            BotOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Clear,
            });
        }

        public override Action GetNextAction()
        {
            try
            {
                var goalEnemy = BotOwner.Memory.GoalEnemy;
                if (goalEnemy == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:!HaveEnemy");
                }

                var haveBullets = BotOwner?.WeaponManager?.HaveBullets;

                if (haveBullets.Value && IsShootFromCoverConditionAllFine())
                {
                    return new Action(typeof(ShootFromCoverLogic), "Mcs:ShootFromCover");
                }

                if (BotOwner.NearDoorData.RecentlyClosedDoorCheckTime + 0.3f < Time.time && BotOwner.BotsGroup.EnemyLastSeenTimeReal + 7f >= Time.time && GetCrossPoint(goalEnemy))
                {
                    BotOwner.Memory.Spotted(false, null, null);
                }

                if (ShouldUseMeleeAttack())
                {
                    return new Action(typeof(MeleeAttackLogic), "Mcs:MeleeAttack");
                }

                if (!haveBullets.Value)
                {
                    BotOwner.WeaponManager.Reload.McsTryReload();
                }

                if (BotOwner.WeaponManager.UnderbarrelLauncherController.NeedToReload())
                {
                    BotOwner.WeaponManager.UnderbarrelLauncherController.TryReload();
                }

                var canShoot = goalEnemy.CanShoot;
#if DEBUG
                // if (canShoot)
                // {
                //     MiyakoCarryServicePlugin.Logger.LogWarning($"CanShoot: {canShoot}, Distance: {goalEnemy.Distance}, IsVisible: {goalEnemy.IsVisible}");
                // }
#endif
                var isProtectWantKill = ProtectWantKill();
                var isProtectCareKill = ProtectCareKill();

                UpdateCoverToShoot();

                if (!goalEnemy.IsVisible && BotOwner.SmokeGrenade.ShallShoot() && BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position) <= 40f * 40f)
                {
                    return new Action(typeof(ShootToSmokeLogic), "Mcs:SmokeGrenad");
                }
                else
                {
                    var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
                    if (mcsLeadPlayerPos == null)
                    {
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Uninitialized");
                    }

                    var safeFire = false;
                    if (canShoot)
                    {
                        var closestFriend = BotOwner.Covers.GetClosestFriend(out var sqrDist);
                        safeFire = sqrDist >= 1f || closestFriend == null || closestFriend.Id > BotOwner.Id;
                    }

                    if (safeFire && haveBullets.Value)
                    {
                        if (goalEnemy.IsVisible)
                        {
                            if (!BotOwner.GoToSomePointData.IsCome())
                            {
                                return new Action(typeof(AttackMovingLogic), "Mcs:AttackMoving");
                            }
                            else
                            {
                                return new Action(typeof(ShootFromPlaceLogic), "Mcs:ShootFromPlace");
                            }
                        }
                        else
                        {
                            if (McsBotPlayerData != null)
                            {
                                if (McsBotPlayerData.HasDecision(EDecision.ShouldGoToPoint))
                                {
                                    if (_nextUpdatePosTime < Time.time)
                                    {
                                        UpdateCommonMoveTarget(McsBotPlayerData.TargetPos, out float nextTime);
                                        _nextUpdatePosTime = Time.time + nextTime;
                                    }

                                    if (_currentMoveTarget.HasValue)
                                    {
                                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                        return new Action(typeof(GoToPointLogic), "Mcs:GoToPointCommand");
                                    }
                                    else
                                    {
                                        return new Action(typeof(SimplePatrolLogic), "Mcs:GoToPointCommandTargetPosNotFound");
                                    }
                                }

                                if (McsBotPlayerData.HasDecision(EDecision.ShouldHoldPosition))
                                {
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionCommand");
                                }
                            }

                            var sqrDistance = BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos);
                            var tooClose = sqrDistance <= TOO_CLOSE_FROM_LEAD_DISTANCE * TOO_CLOSE_FROM_LEAD_DISTANCE;

                            if (_nextUpdatePosTime < Time.time)
                            {
                                UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                                _nextUpdatePosTime = Time.time + nextTime;
                            }

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
                                if (_nextPatrolTime + 4f < Time.time)
                                {
                                    _nextPatrolTime = Time.time + 4f;
                                    if (_currentMoveTarget.HasValue)
                                    {
                                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                        return new Action(typeof(GoToPointLogic), "Partoling");
                                    }

                                    return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindPath2");
                                }
                                else
                                {
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPosition");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (McsBotPlayerData != null && ((mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) <= 50f * 50f && !McsBotPlayerData.HasDecision(EDecision.ShouldRegroup)) || mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) <= 20f * 20f))
                        {
                            return new Action(typeof(RunToEnemyLogic), "Mcs:RushEnemy");
                        }
                        else
                        {
                            if (McsBotPlayerData != null)
                            {
                                if (McsBotPlayerData.HasDecision(EDecision.ShouldGoToPoint))
                                {
                                    if (_nextUpdatePosTime < Time.time)
                                    {
                                        UpdateCommonMoveTarget(McsBotPlayerData.TargetPos, out float nextTime);
                                        _nextUpdatePosTime = Time.time + nextTime;
                                    }

                                    if (_currentMoveTarget.HasValue)
                                    {
                                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                        return new Action(typeof(GoToPointLogic), "Mcs:GoToPointCommand");
                                    }
                                    else
                                    {
                                        return new Action(typeof(SimplePatrolLogic), "Mcs:GoToLootTargetPosNotFound");
                                    }
                                }

                                if (McsBotPlayerData.HasDecision(EDecision.ShouldHoldPosition))
                                {
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionCommand");
                                }
                            }

                            if (_nextUpdatePosTime < Time.time)
                            {
                                UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
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

                                return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindPath3");
                            }
                            else
                            {
                                if (_nextPatrolTime + 4f < Time.time)
                                {
                                    _nextPatrolTime = Time.time + 4f;
                                    if (_currentMoveTarget.HasValue)
                                    {
                                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                        return new Action(typeof(GoToPointLogic), "Partoling");
                                    }

                                    return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindPath4");
                                }
                                else
                                {
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPosition");
                                }
                            }
                        }
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

            if (BotOwner.Memory.HaveEnemy)
            {
                // 使护航下的Zyriachy无视目标处于灯塔限定区域时才可视为敌人的限制
                if (BotOwner.Profile.Info.Settings.Role is WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
                {
                    var goalEnemy = BotOwner.Memory.GoalEnemy;
                    if (goalEnemy != null && goalEnemy.Person != null)
                    {
                        if (BotOwner.Boss.BossLogic is ZyriachyBossLogicClass zyriachyBossLogicClass)
                        {
                            zyriachyBossLogicClass.AddEnemy(goalEnemy.Person, EBotEnemyCause.zryachiyLogic);
                        }
                    }
                }
                return true;
            }

            return false;
        }
    }
}