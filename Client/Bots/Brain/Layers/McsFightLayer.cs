
using System;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsFightLayer : McsBaseLayer
    {
        public float _contactTime = 0f;
        public float _nextRecalcGoalTime = 0f;
        public const float FightHoldTime = 3f;
        public float _lastHaveEnemyTime = -999f;
        public bool _deferToSain = false;

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
                _contactTime = Time.time;
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

            if (Time.time > _contactTime + 0.5f)
            {
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Clear,
                });
            }
        }

        public override Action GetNextAction()
        {
            try
            {
                var time = Time.time;
                var goalEnemy = BotOwner.Memory.GoalEnemy;
                if (!MiyakoCarryServicePlugin.SAINInstalled)
                {
                    if (goalEnemy != null && (goalEnemy.Person == null || goalEnemy.Person.HealthController == null || !goalEnemy.Person.HealthController.IsAlive || goalEnemy.Person.AIData.BotOwner.Brain == null || goalEnemy.Person.AIData.BotOwner.BotState is EBotState.NonActive))
                    {
                        BotOwner.Memory.GoalEnemy = null;
                        if (BotOwner.EnemiesController.EnemyInfos.ContainsKey(goalEnemy.Person))
                        {
                            BotOwner.EnemiesController.Remove(goalEnemy.Person);
                        }
                        _nextRecalcGoalTime = 0f;
                    }

                    if (time >= _nextRecalcGoalTime)
                    {
                        _nextRecalcGoalTime = time + 0.1f;
                        BotOwner.CalcGoal();
                    }

                    goalEnemy = BotOwner.Memory.GoalEnemy;
                }

                var needHeal = (BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use);
                var isEnemyPosLost = IsEnemyPosLost();

                if (McsBotPlayerData != null && McsBotPlayerData.HasDecision(Decisions.ShouldUseStationaryWeapon))
                {
                    var stationary = BotOwner.WeaponManager.Stationary;
                    if (McsBotPlayerData.ProxyTargetId != _cachedProxyTargetId)
                    {
                        _cachedProxyTargetId = McsBotPlayerData.ProxyTargetId;
                        _cachedStationaryWeaponData = Singleton<GameWorld>.Instance.FindInteractableObjectData(McsBotPlayerData.ProxyTargetId) as StationaryWeaponData;
                    }

                    if (_cachedStationaryWeaponData.StationaryWeapon != null && _cachedStationaryWeaponData.StationaryWeaponLink != null)
                    {
                        var stationaryWeapon = _cachedStationaryWeaponData.StationaryWeapon;
                        var stationaryWeaponLink = _cachedStationaryWeaponData.StationaryWeaponLink;
                        var operatorPos = stationaryWeapon.OperatorPosition;
                        var sqrToOperator = BotOwner.Position.McsSqrDistance(operatorPos);

                        if (needHeal && isEnemyPosLost)
                        {
                            if (stationary.CurLink != null && stationary.Taken)
                            {
                                stationary.DropCurWeapon(false, true);
                            }
                            RefreshStuckTimer();
                            return new Action(typeof(StationaryHealLogic), "Mcs:StationaryHealing");
                        }

                        if (stationary.CurLink == null)
                        {
                            stationary.SetTargetStationary(stationaryWeaponLink);
                        }

                        if (_nextUpdatePosTime < time)
                        {
                            UpdateCommonMoveTarget(operatorPos, out float nextTime);
                            _nextUpdatePosTime = time + nextTime;
                        }

                        if (sqrToOperator >= 1.5f && _currentMoveTarget.HasValue)
                        {
                            BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                            return new Action(typeof(GoToPointLogic), "Mcs:GoToStationaryPos");
                        }

                        var isEnemyAtSector = stationary.IsEnemyAtSector(stationary.CurLink);

                        if (stationaryWeaponLink.HaveAmmo() && (goalEnemy == null || (isEnemyAtSector && stationary.GetCurrentDecision() == BotLogicDecision.shootFromStationary && goalEnemy.CanShoot && IsTargetPitchReachable(stationaryWeapon, goalEnemy.CurrPosition))))
                        {
                            return new Action(typeof(ShootFromStationaryLogic), "Mcs:UseStationaryWeapon");
                        }

                        if (goalEnemy == null)
                        {
                            ScanSector(stationaryWeaponLink);
                        }
                    }
                }

                if (goalEnemy == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:!HaveEnemy");
                }

                var haveBullets = BotOwner?.WeaponManager?.HaveBullets;
                if (haveBullets.Value && IsShootFromCoverConditionAllFine())
                {
                    return new Action(typeof(ShootFromCoverLogic), "Mcs:ShootFromCover");
                }

                if (BotOwner.NearDoorData.RecentlyClosedDoorCheckTime + 0.3f < time && BotOwner.BotsGroup.EnemyLastSeenTimeReal + 7f >= time && GetCrossPoint(goalEnemy))
                {
                    BotOwner.Memory.Spotted(false, null, null);
                }

                if (!CheckFirearmsAnimatorState())
                {
                    BotOwner.TryResetHandsState();
                }

                if (ShouldUseMeleeAttack())
                {
                    return new Action(typeof(MeleeAttackLogic), "Mcs:MeleeAttack");
                }

                if (!haveBullets.Value)
                {
                    BotOwner.WeaponManager.Reload.McsTryReload();
                }
                else if (!goalEnemy.CanShoot && BotOwner.McsGetCurrentMagAmmoRatio() <= 0.3f)
                {
                    BotOwner.WeaponManager.Reload.McsTryReload();
                }

                if (BotOwner.WeaponManager.UnderbarrelLauncherController.NeedToReload())
                {
                    BotOwner.WeaponManager.UnderbarrelLauncherController.TryReload();
                }

                var canShoot = goalEnemy.CanShoot;
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
                        return new Action(typeof(HoldPositionLogic), "Mcs:Uninitialized");
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
                                    if (needHeal && isEnemyPosLost)
                                    {
                                        RefreshStuckTimer();
                                        return new Action(typeof(HealLogic), "Mcs:FightHealing1");
                                    }
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionCommand");
                                }
                            }

                            var sqrDistance = BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos);
                            var tooClose = sqrDistance <= TOO_CLOSE_FROM_LEAD_DISTANCE * TOO_CLOSE_FROM_LEAD_DISTANCE;

                            if (_nextUpdatePosTime < time)
                            {
                                UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                                _nextUpdatePosTime = time + nextTime;
                            }

                            if (needHeal && isEnemyPosLost)
                            {
                                RefreshStuckTimer();
                                if (_currentMoveTarget.HasValue)
                                {
                                    BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                }
                                return new Action(typeof(HealLogic), "Mcs:FightHealing2");
                            }

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
                                if (_nextPatrolTime + 4f < time)
                                {
                                    _nextPatrolTime = time + 4f;
                                    if (_currentMoveTarget.HasValue)
                                    {
                                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                        return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                                    }

                                    return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindPath2");
                                }
                                else
                                {
                                    if (needHeal && isEnemyPosLost)
                                    {
                                        RefreshStuckTimer();
                                        return new Action(typeof(HealLogic), "Mcs:FightHealing3");
                                    }
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPosition");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (McsBotPlayerData != null && ((mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) <= 50f * 50f && !McsBotPlayerData.HasDecision(Decisions.ShouldFollowMe)) || mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) <= 20f * 20f) && !McsBotPlayerData.HasDecision(Decisions.ShouldKeepFormation) && !McsBotPlayerData.HasDecision(Decisions.ShouldUseStationaryWeapon))
                        {
                            return new Action(typeof(RunToEnemyLogic), "Mcs:RushEnemy");
                        }
                        else
                        {
                            if (McsBotPlayerData != null)
                            {
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
                                        return new Action(typeof(HoldPositionLogic), "Mcs:GoToLootTargetPosNotFound");
                                    }
                                }

                                if (McsBotPlayerData.HasDecision(Decisions.ShouldHoldPosition))
                                {
                                    if (needHeal && isEnemyPosLost)
                                    {
                                        RefreshStuckTimer();
                                        return new Action(typeof(HealLogic), "Mcs:FightHealing4");
                                    }
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionCommand");
                                }
                            }

                            if (_nextUpdatePosTime < time)
                            {
                                UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                                _nextUpdatePosTime = time + nextTime;
                            }

                            if (needHeal && isEnemyPosLost)
                            {
                                RefreshStuckTimer();
                                if (_currentMoveTarget.HasValue)
                                {
                                    BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                }
                                return new Action(typeof(HealLogic), "Mcs:FightHealing5");
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

                                return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindPath3");
                            }
                            else
                            {
                                if (_nextPatrolTime + 4f < time)
                                {
                                    _nextPatrolTime = time + 4f;
                                    if (_currentMoveTarget.HasValue)
                                    {
                                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                        return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                                    }

                                    return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindPath4");
                                }
                                else
                                {
                                    if (needHeal && isEnemyPosLost)
                                    {
                                        RefreshStuckTimer();
                                        return new Action(typeof(HealLogic), "Mcs:FightHealing6");
                                    }
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

            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null && mcsBotPlayerData.HasDecision(Decisions.ShouldUseStationaryWeapon))
            {
                return true;
            }

            if (BotOwner.Memory.HaveEnemy)
            {
                var goalEnemy = BotOwner.Memory.GoalEnemy;
                var enemyExist = goalEnemy != null && goalEnemy.Person != null;
                // 使护航下的Zyriachy无视目标处于灯塔限定区域时才可视为敌人的限制
                if (BotOwner.Profile.Info.Settings.Role is WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
                {
                    if (enemyExist)
                    {
                        if (BotOwner.Boss.BossLogic is ZyriachyBossLogicClass zyriachyBossLogicClass)
                        {
                            zyriachyBossLogicClass.AddEnemy(goalEnemy.Person, EBotEnemyCause.zryachiyLogic);
                        }
                    }
                }

                if (mcsBotPlayerData == null)
                {
                    _lastHaveEnemyTime = -999f;
                    return false;
                }

                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(mcsBotPlayerData);
                if (enemyExist && MiyakoCarryServicePlugin.SAINInstalled)
                {
                    var sqrDist = mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position);
                    if (_deferToSain)
                    {
                        if (sqrDist > SAINUtils.ExitSainSqr)
                        {
                            _deferToSain = false;
                        }
                    }
                    else
                    {
                        if (sqrDist < SAINUtils.EnterSainSqr)
                        {
                            _deferToSain = true;
                        }
                    }

                    if (_deferToSain)
                    {
                        return false;
                    }
                }

                _lastHaveEnemyTime = Time.time;
                return true;
            }

            if (Time.time - _lastHaveEnemyTime < FightHoldTime)
            {
                return true;
            }

            return false;
        }

        public override bool IsEnemyPosLost()
        {
            if (Time.time - BotOwner.Memory.LastEnemyTimeSeen > 5f)
            {
                return true;
            }
            return base.IsEnemyPosLost();
        }
    }
}