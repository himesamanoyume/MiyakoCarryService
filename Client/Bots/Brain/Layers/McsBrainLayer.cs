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
    public class McsBrainLayer : McsBaseLayer
    {
        public McsBrainLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public float _contactTime = 0f;
        public float _nextRecalcGoalTime = 0f;
        public const float FightHoldTime = 3f;
        public bool _deferToSain = false;
        public float _goToStationaryStuckTime = -999f;
        public float _lastSqrToOperator = float.MaxValue;
        public float _lastCanShootTime = -999f;
        private const float CAN_SHOOT_HOLD_TIME = 2f;
        private const float CAN_SHOOT_HOLD_TIME_FREE = 15f;
        private const float ARRIVE_DIST = 2.5f;
        private const float LOOK_AROUND_TIME = 2f;
        private const float STUCK_TIMEOUT = 8f;
        public int _isTurnRight = 1;

        private static readonly string[] _travelTaskIntents =
        {
            Intents.ShouldQuestProxyAction,
            Intents.ShouldLootProxyAction,
            Intents.ShouldInteractionProxyAction,
            Intents.ShouldStationaryWeaponProxyAction,
            Intents.ShouldEscort,
            Intents.ShouldEscortToBtr,
            Intents.ShouldGoToPoint,
            Intents.ShouldDropTargetLoot,
        };

        public override Action GetNextAction()
        {
            try
            {
                var time = Time.time;
                var mcsLeadPlayerPos = Vector3.zero;
                var sqrDistance = 0f;
                var tooClose = false;
                var needHeal = false;
                var goalEnemy = BotOwner.Memory.GoalEnemy;
                var canShootNow = CanShootNow();
                if (canShootNow)
                {
                    _lastCanShootTime = time;
                }

                #region McsAvoidDangerLayer

                if (BotOwner.FlashGrenade.IsFlashed)
                {
                    return new Action(typeof(FlashedLogic), "Mcs:Flashed");
                }

                if (BotOwner.BotTurnAwayLight.IsActive)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:TurnAwayLight");
                }

                if (BotOwner.ArtilleryDangerPlace.ShallRunAway())
                {
                    return new Action(typeof(RunAwayArtilleryLogic), "Mcs:RunAwayArtillery");
                }

                if (BotOwner.BewareGrenade.McsShallRunAway())
                {
                    return new Action(typeof(RunAwayGrenadeLogic), "Mcs:RunAwayGrenade");
                }

                if (BotOwner.BewareBTR.ShallRunAway())
                {
                    return new Action(typeof(RunAwayBTRLogic), "Mcs:RunAwayBTR");
                }

                if (!BotOwner.Memory.HaveEnemy && BotOwner.SmokeGrenade.IsInSmoke)
                {
                    return new Action(typeof(GoToCoverPointLogic), "Mcs:PeaceSmoke");
                }

                if (BotOwner.BewarePlantedMine.CanDeactivate())
                {
                    return new Action(typeof(DeactivateMineLogic), "Mcs:DeactivateMine");
                }

                #endregion

                if (McsBotPlayerData == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:LeadPosNull");
                }

                var hasTravelTask = McsBotPlayerData.HasAnyIntent(_travelTaskIntents);
                var fightActive = goalEnemy != null && time - _lastCanShootTime <= (hasTravelTask ? CAN_SHOOT_HOLD_TIME : CAN_SHOOT_HOLD_TIME_FREE);
                needHeal = (BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use);
                var isEnemyPosLost = IsEnemyPosLost();
                mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
                sqrDistance = BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos);
                tooClose = sqrDistance <= TOO_CLOSE_FROM_LEAD_DISTANCE * TOO_CLOSE_FROM_LEAD_DISTANCE;

                #region McsProxyLayer

                if (!fightActive && McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
                {
                    if (McsBotPlayerData.HasIntent(Intents.ShouldQuestProxyAction)
                        || McsBotPlayerData.HasIntent(Intents.ShouldLootProxyAction)
                        || McsBotPlayerData.HasIntent(Intents.ShouldInteractionProxyAction)
                        || McsBotPlayerData.HasIntent(Intents.ShouldStationaryWeaponProxyAction))
                    {

                        if (McsBotPlayerData.HasIntent(Intents.ShouldHoldPosition))
                        {
                            return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionForProxyAction");
                        }

                        if (McsBotPlayerData.TargetPos.HasValue)
                        {
                            if (TryRefreshCommonTarget(McsBotPlayerData.TargetPos, time))
                            {
                                ApplyMovePoint();
                                if (needHeal)
                                {
                                    RefreshStuckTimer();
                                    return new Action(typeof(HealLogic), "Mcs:HealWhileProxy");
                                }
                                return new Action(typeof(GoToExcuteProxyActionLogic), "Mcs:GoToExcuteProxyAction");
                            }
                        }
                    }
                }

                #endregion
                #region McsEscortLayer

                if (!fightActive && McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
                {
                    if (McsBotPlayerData.HasIntent(Intents.ShouldEscortToBtr))
                    {
                        var btrController = Singleton<GameWorld>.Instance.BtrController;
                        var side = btrController.BtrView.GetBtrSide(1);
                        if (side == null)
                        {
                            return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindBtrSide");
                        }

                        var doorPos = side.GoInPoints().Item1;
                        if (_nextUpdatePosTime < time)
                        {
                            McsBotPlayerData.TargetPos = doorPos;
                            UpdateEscortMoveTarget(McsBotPlayerData.TargetPos, out float nextTime);
                            _nextUpdatePosTime = time + nextTime;
                        }

                        if (_currentMoveTarget.HasValue)
                        {
                            ApplyMovePoint();
                            if (needHeal)
                            {
                                RefreshStuckTimer();
                                return new Action(typeof(HealLogic), "Mcs:HealWhileEscort");
                            }
                            return new Action(typeof(EscortToPointByWayLogic), "Mcs:EscortToBtr");
                        }
                    }

                    if ((McsBotPlayerData.HasIntent(Intents.ShouldEscort) && McsBotPlayerData.TargetPos.HasValue) || McsBotPlayerData.HasIntent(Intents.ShouldEscortToBtr))
                    {
                        if (TryRefreshEscortTarget(McsBotPlayerData.TargetPos, time))
                        {
                            ApplyMovePoint();
                            if (needHeal)
                            {
                                RefreshStuckTimer();
                                return new Action(typeof(HealLogic), "Mcs:HealWhileEscort");
                            }
                            return new Action(typeof(EscortToPointByWayLogic), "Mcs:EscortToPoint");
                        }
                    }
                }

                #endregion
                #region StationaryWeapon

                if (McsBotPlayerData.HasIntent(Intents.ShouldUseStationaryWeapon))
                {
                    var stationary = BotOwner.WeaponManager.Stationary;
                    if (_cachedProxyTargetId == null || _cachedStationaryWeaponData == null || McsBotPlayerData.ProxyTargetId != _cachedProxyTargetId)
                    {
                        _cachedProxyTargetId = McsBotPlayerData.ProxyTargetId;
                        _cachedStationaryWeaponData = Singleton<GameWorld>.Instance.FindInteractableObjectData(McsBotPlayerData.ProxyTargetId) as StationaryWeaponData;
                    }

                    var stationaryWeapon = _cachedStationaryWeaponData?.StationaryWeapon;
                    var stationaryWeaponLink = _cachedStationaryWeaponData?.StationaryWeaponLink;

                    if (_cachedProxyTargetId != null && stationaryWeapon != null && stationaryWeaponLink != null)
                    {
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

                        TryRefreshCommonTarget(operatorPos, time);

                        if (sqrToOperator >= 1.5f && _currentMoveTarget.HasValue)
                        {
                            if (sqrToOperator < _lastSqrToOperator - 0.5f)
                            {
                                _goToStationaryStuckTime = time;
                                _lastSqrToOperator = sqrToOperator;
                            }
                            else if (_goToStationaryStuckTime <= 0f || _goToStationaryStuckTime > time)
                            {
                                _goToStationaryStuckTime = time;
                                _lastSqrToOperator = sqrToOperator;
                            }

                            if (sqrToOperator < 9f && time - _goToStationaryStuckTime > 8f)
                            {
                                BotOwner.StopMove();
                                BotOwner.Mover.AllowTeleport();
                                BotOwner.GetPlayer.Teleport(operatorPos, true);
                                BotOwner.Mover._lastGoodCastPoint = BotOwner.Mover._prevSuccessLinkedFrom = BotOwner.Mover._prevLinkPos = BotOwner.Mover.PositionOnWayInner = operatorPos;
                                BotOwner.Mover._lastGoodCastPointTime = time;
                                BotOwner.Mover._prevPosLinkedTime = 0f;
                                BotOwner.Mover.SetPlayerToNavMesh(operatorPos);
                                BotOwner.Mover.RecalcWay();
                                BotOwner.Mover.Pause = true;

                                _goToStationaryStuckTime = time;
                                _lastSqrToOperator = float.MaxValue;
                                RefreshStuckTimer();

                                if (stationary.CurLink == null)
                                {
                                    stationary.SetTargetStationary(stationaryWeaponLink);
                                }
                                return new Action(typeof(GoToPointLogic), "Mcs:GoToStationaryPos");
                            }

                            ApplyMovePoint();
                            return new Action(typeof(GoToPointLogic), "Mcs:GoToStationaryPos");
                        }
                        else
                        {
                            _goToStationaryStuckTime = -999f;
                            _lastSqrToOperator = float.MaxValue;
                        }

                        var isEnemyAtSector = stationary.IsEnemyAtSector(stationary.CurLink);

                        if (stationaryWeaponLink.HaveAmmo() && (goalEnemy == null || (isEnemyAtSector && stationary.GetCurrentDecision() == BotLogicDecision.shootFromStationary && goalEnemy.CanShoot && IsTargetPitchReachable(stationaryWeapon, goalEnemy.CurrPosition))))
                        {
                            BotOwner.ShootData.EndShoot();
                            return new Action(typeof(ShootFromStationaryLogic), "Mcs:UseStationaryWeapon");
                        }

                        if (goalEnemy == null)
                        {
                            ScanSector(stationaryWeaponLink);
                        }
                    }
                }
                else
                {
                    _goToStationaryStuckTime = -999f;
                    _lastSqrToOperator = float.MaxValue;
                }

                if (McsBotPlayerData.HasIntent(Intents.ShouldUseStationaryWeapon) && goalEnemy == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:ScanSector");
                }

                #endregion
                #region McsFightLayer

                if (fightActive)
                {
                    if (!MiyakoCarryServicePlugin.SAINInstalled || SAINUtils.GetSAINBot(BotOwner) == null)
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

                    var isProtectWantKill = ProtectWantKill();
                    var isProtectCareKill = ProtectCareKill();

                    UpdateCoverToShoot();

                    if (!goalEnemy.IsVisible && BotOwner.SmokeGrenade.ShallShoot() && BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position) <= 40f * 40f)
                    {
                        return new Action(typeof(ShootToSmokeLogic), "Mcs:SmokeGrenad");
                    }
                    else
                    {
                        if (mcsLeadPlayerPos == null)
                        {
                            return new Action(typeof(HoldPositionLogic), "Mcs:Uninitialized");
                        }

                        var safeFire = false;
                        if (canShootNow)
                        {
                            var closestFriend = BotOwner.Covers.GetClosestFriend(out var sqrDist);
                            safeFire = sqrDist >= 1f || closestFriend == null || closestFriend.Id > BotOwner.Id;
                        }

                        if (safeFire && haveBullets.Value)
                        {
                            if (goalEnemy.IsVisible)
                            {
                                if (!BotOwner.GoToSomePointData.IsCome() && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation))
                                {
                                    return new Action(typeof(AttackMovingLogic), "Mcs:AttackMoving");
                                }
                                else
                                {
                                    return new Action(typeof(ShootFromPlaceLogic), "Mcs:ShootFromPlace");
                                }
                            }
                        }
                        else
                        {
                            if (!hasTravelTask
                                && ((mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) <= 50f * 50f && !McsBotPlayerData.HasIntent(Intents.ShouldFollowMe)) || mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) <= 20f * 20f)
                                && !McsBotPlayerData.HasAnyIntent(Intents.ShouldKeepFormation, Intents.ShouldUseStationaryWeapon, Intents.ShouldHoldPosition))
                            {
                                return new Action(typeof(RunToEnemyLogic), "Mcs:RushEnemy");
                            }
                            else
                            {
                                if (McsBotPlayerData.HasIntent(Intents.ShouldGoToPoint))
                                {
                                    if (TryRefreshCommonTarget(McsBotPlayerData.TargetPos, time))
                                    {
                                        ApplyMovePoint();
                                        return new Action(typeof(GoToPointLogic), "Mcs:GoToPointCommand");
                                    }
                                    else
                                    {
                                        return new Action(typeof(HoldPositionLogic), "Mcs:GoToLootTargetPosNotFound");
                                    }
                                }

                                if (McsBotPlayerData.HasIntent(Intents.ShouldHoldPosition))
                                {
                                    if (needHeal && isEnemyPosLost)
                                    {
                                        RefreshStuckTimer();
                                        return new Action(typeof(HealLogic), "Mcs:FightHealing4");
                                    }
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionCommand");
                                }

                                TryRefreshLeadTarget(mcsLeadPlayerPos, time);

                                if (needHeal && isEnemyPosLost)
                                {
                                    RefreshStuckTimer();
                                    ApplyMovePoint();
                                    return new Action(typeof(HealLogic), "Mcs:FightHealing5");
                                }

                                if (sqrDistance >= TOO_FAR_FROM_LEAD_DISTANCE * 1 || tooClose)
                                {
                                    if (_currentMoveTarget.HasValue)
                                    {
                                        ApplyMovePoint();
                                        return new Action(typeof(GoToPointLogic), tooClose ? "Mcs:TooClose" : "Mcs:TooFar");
                                    }
                                }
                                else
                                {
                                    if (_nextPatrolTime + 4f < time)
                                    {
                                        _nextPatrolTime = time + 4f;
                                        if (_currentMoveTarget.HasValue)
                                        {
                                            ApplyMovePoint();
                                            return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                                        }
                                    }
                                    else
                                    {
                                        if (needHeal && isEnemyPosLost)
                                        {
                                            RefreshStuckTimer();
                                            return new Action(typeof(HealLogic), "Mcs:FightHealing6");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion
                #region McsExfiltrationLayer

                if (!McsBotPlayerData.LeadPlayer.HealthController.IsAlive || McsBotPlayerData.HasIntent(Intents.ShouldExfil))
                {
                    if (BotOwner.PatrollingData.ExfiltrationData.HaveActions())
                    {
                        return new Action(typeof(GoToExfiltrationPointLogic), "Mcs:GotoExit");
                    }
                }

                #endregion
                #region McsClearAreaLayer

                if (McsBotPlayerData.HasIntent(Intents.ShouldClearArea) && McsBotPlayerData.ClearAreaPoints != null && McsBotPlayerData.ClearAreaPoints.Count > 0)
                {
                    if (McsBotPlayerData.ClearAreaIndex >= McsBotPlayerData.ClearAreaPoints.Count)
                    {
                        FinishClearArea();
                        return new Action(typeof(HoldPositionLogic), "Mcs:ClearAreaDone");
                    }

                    var targetPos = McsBotPlayerData.ClearAreaPoints[McsBotPlayerData.ClearAreaIndex];
                    McsBotPlayerData.TargetPos = targetPos;

                    var arrived = BotOwner.Position.McsSqrDistance(targetPos) <= ARRIVE_DIST * ARRIVE_DIST;
                    var stuck = BotOwner.Mover._lastTimePosChanged + STUCK_TIMEOUT < time;

                    if (arrived || stuck)
                    {
                        if (arrived && LOOK_AROUND_TIME > 0f)
                        {
                            if (McsBotPlayerData.ClearAreaLookAroundUntil <= 0f && MyExtensions.IsTrue100(30f))
                            {
                                _isTurnRight = MyExtensions.RandomSing();
                                McsBotPlayerData.ClearAreaLookAroundUntil = time + LOOK_AROUND_TIME;
                                BotOwner.StopMove();
                            }

                            if (time < McsBotPlayerData.ClearAreaLookAroundUntil)
                            {
                                var yaw = time * 90f % 360f * _isTurnRight;
                                var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                                BotOwner.Steering.LookToDirection(dir, 120f);
                                return new Action(typeof(HoldPositionLogic), "Mcs:ClearAreaLookAround");
                            }
                        }

                        McsBotPlayerData.ClearAreaLookAroundUntil = 0f;
                        McsBotPlayerData.ClearAreaIndex++;
                        BotOwner.Mover._lastTimePosChanged = time;

                        if (McsBotPlayerData.ClearAreaIndex >= McsBotPlayerData.ClearAreaPoints.Count)
                        {
                            FinishClearArea();
                            return new Action(typeof(HoldPositionLogic), "Mcs:ClearAreaDone");
                        }

                        targetPos = McsBotPlayerData.ClearAreaPoints[McsBotPlayerData.ClearAreaIndex];
                        McsBotPlayerData.TargetPos = targetPos;
                    }

                    if (TryRefreshCommonTarget(McsBotPlayerData.TargetPos, time))
                    {
                        ApplyMovePoint();
                        return new Action(typeof(GoToPointLogic), "Mcs:ClearAreaGoToPoint");
                    }
                }

                #endregion
                #region McsCommonLayer

                if (mcsLeadPlayerPos == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:LeadPosNull");
                }

                if (McsBotPlayerData.HasIntent(Intents.ShouldDropTargetLoot) && BotOwner.ExternalItemsController.HaveItemsToDrop())
                {
                    if (TryRefreshLeadTarget(mcsLeadPlayerPos, time))
                    {
                        ApplyMovePoint();
                        return new Action(typeof(DropTargetLootLogic), "Mcs:DropTargetLootCommand");
                    }
                }

                if (McsBotPlayerData.HasIntent(Intents.ShouldGoToPoint))
                {
                    if (TryRefreshCommonTarget(McsBotPlayerData.TargetPos, time))
                    {
                        ApplyMovePoint();
                        return new Action(typeof(GoToPointLogic), "Mcs:GoToPointCommand");
                    }
                }

                if (McsBotPlayerData.HasIntent(Intents.ShouldHoldPosition))
                {
                    if (needHeal)
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

                if (!BotOwner.WeaponManager.Reload.Reloading)
                {
                    var haveBullets = BotOwner.WeaponManager.HaveBullets;
                    if (!haveBullets || BotOwner.McsGetCurrentMagAmmoRatio() <= 0.3f)
                    {
                        BotOwner.WeaponManager.Reload.McsTryReload();
                    }
                }

                if (needHeal)
                {
                    TryRefreshLeadTarget(mcsLeadPlayerPos, time);
                    RefreshStuckTimer();
                    ApplyMovePoint();
                    return new Action(typeof(HealLogic), "Mcs:CommonHealing2");
                }
                else if (TryGetBtrFollowAction(time, out var btrAction))
                {
                    return btrAction;
                }
                else if (_nextLootingCheckTime < time && McsBotPlayerData.LootingTarget != null && !McsBotPlayerData.HasIntent(Intents.ShouldFollowMe))
                {
                    var enableLooting = McsBotPlayerData.McsAILeadPlayer.McsBotPlayerConfig.EnableLooting;
                    var hasEmergencyLootNeed = McsBotPlayerData.HasEmergencyLootNeed();

                    if (!enableLooting && !hasEmergencyLootNeed)
                    {
                        McsBotPlayerData.IsLooting = false;
                    }
                    else
                    {
                        if (TryRefreshCommonTarget(McsBotPlayerData.LootingTarget.RootTransform.position, time))
                        {
                            ApplyMovePoint();
                            return new Action(typeof(GoToLootTargetLogic), "Mcs:GoToLootTarget");
                        }
                    }
                }

                TryRefreshLeadTarget(mcsLeadPlayerPos, time);

                if (sqrDistance >= TOO_FAR_FROM_LEAD_DISTANCE * 1 || tooClose)
                {
                    if (_currentMoveTarget.HasValue)
                    {
                        ApplyMovePoint();
                        return new Action(typeof(GoToPointLogic), tooClose ? "Mcs:TooClose" : "Mcs:TooFar");
                    }
                }
                else
                {
                    if (_nextPatrolTime < time)
                    {
                        _nextPatrolTime = time + 8f;
                        if (_currentMoveTarget.HasValue)
                        {
                            ApplyMovePoint();
                            return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                        }
                    }
                }

                #endregion

                return new Action(typeof(HoldPositionLogic), "Mcs:Default");
            }
            catch (Exception e)
            {
                McsLogger.LogError(e);
                return new Action(typeof(HoldPositionLogic), "Mcs:Exception");
            }
        }

        #region 移动目标刷新辅助（原内联刷新块的行为等价封装）

        private bool TryRefreshCommonTarget(Vector3? targetPos, float time)
        {
            if (_nextUpdatePosTime < time)
            {
                UpdateCommonMoveTarget(targetPos, out float nextTime);
                _nextUpdatePosTime = time + nextTime;
            }
            return _currentMoveTarget.HasValue;
        }

        private bool TryRefreshEscortTarget(Vector3? escortPos, float time)
        {
            if (_nextUpdatePosTime < time)
            {
                UpdateEscortMoveTarget(escortPos, out float nextTime);
                _nextUpdatePosTime = time + nextTime;
            }
            return _currentMoveTarget.HasValue;
        }

        private bool TryRefreshLeadTarget(Vector3? leadPos, float time)
        {
            if (_nextUpdatePosTime < time)
            {
                UpdateLeadNearMoveTarget(leadPos, out float nextTime);
                _nextUpdatePosTime = time + nextTime;
            }
            return _currentMoveTarget.HasValue;
        }

        private void ApplyMovePoint()
        {
            if (_currentMoveTarget.HasValue)
            {
                BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
            }
        }

        #endregion

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

            // 固定武器操控不让渡SAIN（原McsFightLayer.IsActive语义）
            if (mcsBotPlayerData != null && mcsBotPlayerData.HasIntent(Intents.ShouldUseStationaryWeapon))
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
                        if (BotOwner.Boss.BossLogic is BossZryachiy bossZryachiy)
                        {
                            bossZryachiy.AddEnemy(goalEnemy.Person, EBotEnemyCause.zryachiyLogic);
                        }
                    }
                }

                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(mcsBotPlayerData);
                if (enemyExist && MiyakoCarryServicePlugin.SAINInstalled && SAINUtils.GetSAINBot(BotOwner) != null)
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
            }
            return true;
        }

        private void FinishClearArea()
        {
            McsBotPlayerData.ClearAreaPoints = null;
            McsBotPlayerData.ClearAreaIndex = 0;
            McsBotPlayerData.ClearAreaLookAroundUntil = 0f;
            McsBotPlayerData.TargetPos = null;
            McsBotPlayerData.RemoveIntent(Intents.ShouldClearArea);
            BotOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Clear
            });
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