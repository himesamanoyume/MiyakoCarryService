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

        private float _contactTime = 0f;
        private float _nextRecalcGoalTime = 0f;
        private bool _deferToSain = false;
        private float _goToStationaryStuckTime = -999f;
        private float _lastSqrToOperator = float.MaxValue;
        private float _lastCanShootTime = -999f;
        private const float CAN_SHOOT_HOLD_TIME = 2f;
        private const float CAN_SHOOT_HOLD_TIME_FREE = 15f;
        private const float ARRIVE_DIST = 2.5f;
        private const float LOOK_AROUND_TIME = 2f;
        private const float STUCK_TIMEOUT = 8f;
        private int _isTurnRight = 1;

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






        private const float SUPPRESS_TIME_SINCE_SEEN = 3f;



        private const float RUSH_NEAR_ENEMY_SQUARE_DIST = 15f * 15f;



        private const float SHIFT_COVER_MIN_TIME = 6f;




        private const float GOAL_SWITCH_LOCK_TIME = 2f;

        private const float GOAL_SWITCH_ADVANTAGE = 0.75f;

        private string _holdGroundEnemyId = null;
        private float _holdGroundStartTime = 0f;
        private float _holdGroundDuration = 0f;
        private float _lastEnemyVisibleTime = -999f;
        private float _coverEnterTime = 0f;

        private string _lastAcceptedGoalEnemyId = null;
        private float _lastAcceptedGoalSetTime = -999f;


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

                if (McsBotPlayerData.LeadPlayer == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:LeadPlayerNull");
                }

                var hasTravelTask = McsBotPlayerData.HasAnyIntent(_travelTaskIntents);
                var fightActive = (goalEnemy != null && time - _lastCanShootTime <= (hasTravelTask ? CAN_SHOOT_HOLD_TIME : CAN_SHOOT_HOLD_TIME_FREE))
                    || IsApproachingThreat();
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
                            UpdateGoalEnemyWithHysteresis(time);
                        }

                        goalEnemy = BotOwner.Memory.GoalEnemy;
                    }

                    if (goalEnemy == null)
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:FightNoEnemy");
                    }

                    var haveBullets = BotOwner?.WeaponManager?.HaveBullets;

                    TrackCoverEnter(time);

                    if (_coverEnterTime > 0f
                        && time - _coverEnterTime > SHIFT_COVER_MIN_TIME
                        && time - GetEnemyLastSeenTime() > 30f
                        && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation))
                    {
                        if (TryGetCombatCoverTarget(mcsLeadPlayerPos, out var shiftCoverPos))
                        {
                            UpdateCommonMoveTarget(shiftCoverPos, out var shiftNextTime);
                            if (_currentMoveTarget.HasValue)
                            {
                                _coverEnterTime = 0f;
                                ApplyMovePoint();
                                return new Action(typeof(GoToPointLogic), "Mcs:ShiftCover");
                            }
                        }
                        else
                        {
                            _coverEnterTime = time - SHIFT_COVER_MIN_TIME + 5f;
                        }
                    }

                    if (haveBullets.Value && IsShootFromCoverConditionAllFine())
                    {
                        return new Action(typeof(McsCoverPeakLogic), "Mcs:CoverPeak");
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

                    McsBotPlayerData.UpdateSuppressionDecay();
                    TrySelectLastHitShooter(time);

                    TrySelectLeadThreatEnemy(time);

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

                        var botToEnemySqrDist = BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position);
                        var enemyToLeadSqrDist = mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position);

                        if (goalEnemy.IsVisible)
                        {
                            _lastEnemyVisibleTime = time;
                        }

                        if (haveBullets.Value
                            && botToEnemySqrDist <= (15f * 15f)
                            && (time - _lastEnemyVisibleTime <= 1f || time - McsBotPlayerData.LastHitTime <= 2f)
                            && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation, Intents.ShouldUseStationaryWeapon))
                        {
                            return new Action(typeof(McsDogFightLogic), "Mcs:DogFight");
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
                                UpdateHoldGroundTimer(goalEnemy, time);

                                if (botToEnemySqrDist <= (20f * 20f)
                                    && !McsBotPlayerData.IsHeavySuppressed
                                    && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation)
                                    && !BotOwner.GoToSomePointData.IsCome())
                                {
                                    return new Action(typeof(AttackMovingLogic), "Mcs:AttackMovingClose");
                                }

                                var holdGroundExpired = IsEnemyLookingAtMe(goalEnemy) && time - _holdGroundStartTime >= _holdGroundDuration;
                                var heavySuppressed = McsBotPlayerData.IsHeavySuppressed;

                                if ((holdGroundExpired || heavySuppressed)
                                    && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation)
                                    && TryGetCombatCoverTarget(mcsLeadPlayerPos, out var coverPos))
                                {
                                    UpdateCommonMoveTarget(coverPos, out var coverNextTime);
                                    if (_currentMoveTarget.HasValue)
                                    {
                                        ApplyMovePoint();
                                        return new Action(typeof(GoToPointLogic), heavySuppressed ? "Mcs:SuppressedSeekCover" : "Mcs:SeekCover");
                                    }
                                }

                                if (!BotOwner.GoToSomePointData.IsCome() && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation))
                                {
                                    return new Action(typeof(AttackMovingLogic), "Mcs:AttackMoving");
                                }
                                else
                                {
                                    return new Action(typeof(McsStandAndShootLogic), "Mcs:StandAndShoot");
                                }
                            }
                            else
                            {
                                _holdGroundEnemyId = null;

                                if (botToEnemySqrDist <= RUSH_NEAR_ENEMY_SQUARE_DIST && ShallRushEnemy(enemyToLeadSqrDist, goalEnemy, hasTravelTask))
                                {
                                    return new Action(typeof(RunToEnemyLogic), "Mcs:RushEnemyNear");
                                }

                                var timeSinceSeen = time - GetEnemyLastSeenTime();
                                var shotByThisEnemy = IsShotByEnemyRecently(goalEnemy, time);
                                if (timeSinceSeen <= SUPPRESS_TIME_SINCE_SEEN || shotByThisEnemy)
                                {
                                    return new Action(typeof(McsSuppressFireLogic), "Mcs:SuppressFire");
                                }

                                if (ShallRushEnemy(enemyToLeadSqrDist, goalEnemy, hasTravelTask))
                                {
                                    return new Action(typeof(RunToEnemyLogic), "Mcs:RushEnemy");
                                }

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
                        else
                        {
                            if (!haveBullets.Value && TryGetCombatCoverTarget(mcsLeadPlayerPos, out var reloadCoverPos))
                            {
                                UpdateCommonMoveTarget(reloadCoverPos, out var reloadNextTime);
                                if (_currentMoveTarget.HasValue)
                                {
                                    ApplyMovePoint();
                                    return new Action(typeof(GoToPointLogic), "Mcs:ReloadInCover");
                                }
                            }

                            if (ShallRushEnemy(enemyToLeadSqrDist, goalEnemy, hasTravelTask, botToEnemySqrDist))
                            {
                                return new Action(typeof(RunToEnemyLogic), "Mcs:RushEnemy");
                            }

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

                #endregion
                #region McsExfiltrationLayer

                if (McsBotPlayerData.LeadPlayer == null || !McsBotPlayerData.LeadPlayer.HealthController.IsAlive || McsBotPlayerData.HasIntent(Intents.ShouldExfil))
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
                else if (_nextLootingCheckTime < time && McsBotPlayerData.LootingTarget != null && McsBotPlayerData.McsAILeadPlayer != null && !McsBotPlayerData.HasIntent(Intents.ShouldFollowMe))
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


        private void UpdateHoldGroundTimer(EnemyInfo goalEnemy, float time)
        {
            var enemyId = goalEnemy.Person?.ProfileId;
            if (_holdGroundEnemyId != enemyId)
            {
                _holdGroundEnemyId = enemyId;
                _holdGroundStartTime = time;
                _holdGroundDuration = 4f * UnityEngine.Random.Range(0.66f, 1.5f);
            }
        }

        private bool IsEnemyLookingAtMe(EnemyInfo goalEnemy)
        {
            var enemyBotOwner = goalEnemy.Person?.AIData?.BotOwner;
            if (enemyBotOwner != null && enemyBotOwner.BotState != EBotState.NonActive)
            {
                var enemyGoalEnemy = enemyBotOwner.Memory?.GoalEnemy;
                if (enemyGoalEnemy?.Person == null)
                {
                    return false;
                }
                return enemyGoalEnemy.Person.ProfileId == BotOwner.ProfileId;
            }
            return true;
        }

        private void TrySelectLastHitShooter(float time)
        {
            var shooter = McsBotPlayerData.LastHitShooter;
            if (shooter == null || time - McsBotPlayerData.LastHitTime > 2f || !shooter.HealthController.IsAlive)
            {
                return;
            }

            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.Person?.ProfileId == shooter.ProfileId)
            {
                return;
            }

            if (BotOwner.EnemiesController.EnemyInfos.TryGetValue(shooter, out var enemyInfo))
            {
                BotOwner.Memory.GoalEnemy = enemyInfo;
                _nextRecalcGoalTime = 0f;
            }
        }

        private void TrySelectLeadThreatEnemy(float time)
        {
            var mcsAILeadPlayer = McsBotPlayerData.McsAILeadPlayer;
            if (mcsAILeadPlayer == null)
            {
                return;
            }

            var threatEnemy = mcsAILeadPlayer.GetLeadThreatEnemy();
            if (threatEnemy == null)
            {
                return;
            }

            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.Person?.ProfileId == threatEnemy.ProfileId)
            {
                return;
            }

            if (BotOwner.EnemiesController.EnemyInfos.TryGetValue(threatEnemy, out var enemyInfo))
            {
                BotOwner.Memory.GoalEnemy = enemyInfo;
                _nextRecalcGoalTime = 0f;
            }
        }

        private bool TrySelectNearestLeadVisibleEnemy(float time)
        {
            var mcsAILeadPlayer = McsBotPlayerData.McsAILeadPlayer;
            if (mcsAILeadPlayer == null)
            {
                return false;
            }

            var leadVisibleEnemies = mcsAILeadPlayer.LeadVisibleEnemies;
            if (leadVisibleEnemies == null || leadVisibleEnemies.Count == 0)
            {
                return false;
            }

            EnemyInfo nearestEnemyInfo = null;
            var minSqrDistance = float.MaxValue;
            foreach (var leadVisibleEnemy in leadVisibleEnemies)
            {
                if (leadVisibleEnemy == null || !leadVisibleEnemy.HealthController.IsAlive)
                {
                    continue;
                }

                if (!BotOwner.EnemiesController.EnemyInfos.TryGetValue(leadVisibleEnemy, out var enemyInfo))
                {
                    continue;
                }

                var sqrDistance = BotOwner.Position.McsSqrDistance(leadVisibleEnemy.Position);
                if (sqrDistance < minSqrDistance)
                {
                    nearestEnemyInfo = enemyInfo;
                    minSqrDistance = sqrDistance;
                }
            }

            if (nearestEnemyInfo == null)
            {
                return false;
            }

            BotOwner.Memory.GoalEnemy = nearestEnemyInfo;
            _nextRecalcGoalTime = 0f;
            return true;
        }

        private bool IsLeadThreatEnemy(IPlayer enemyPerson)
        {
            var mcsAILeadPlayer = McsBotPlayerData?.McsAILeadPlayer;
            if (mcsAILeadPlayer == null || enemyPerson == null)
            {
                return false;
            }

            return mcsAILeadPlayer.IsLeadThreatEnemy(enemyPerson);
        }

        private bool TryGetCombatCoverTarget(Vector3 mcsLeadPlayerPos, out Vector3 coverPos)
        {
            coverPos = Vector3.zero;
            CustomNavigationPoint coverPoint = null;

            if (_haveCoverToShoot && _currentNavigationPoint != null && _currentNavigationPoint.IsFreeById(BotOwner.Id) && !_currentNavigationPoint.IsSpotted)
            {
                coverPoint = _currentNavigationPoint;
            }
            else
            {
                TryFindCover(mcsLeadPlayerPos);
                if (_currentNavigationPoint != null && !_currentNavigationPoint.IsSpotted)
                {
                    coverPoint = _currentNavigationPoint;
                }
            }

            if (coverPoint == null)
            {
                return false;
            }

            if (BotOwner.Position.McsSqrDistance(coverPoint.Position) > (30f * 30f))
            {
                return false;
            }

            if (mcsLeadPlayerPos.McsSqrDistance(coverPoint.Position) > TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
            {
                return false;
            }

            if (BotOwner.Position.McsSqrDistance(coverPoint.Position) <= (1.5f * 1.5f))
            {
                return false;
            }

            coverPos = coverPoint.Position;
            return true;
        }

        private void TrackCoverEnter(float time)
        {
            if (BotOwner.Memory.IsInCover)
            {
                if (_coverEnterTime <= 0f)
                {
                    _coverEnterTime = time;
                }
            }
            else
            {
                _coverEnterTime = 0f;
            }
        }

        private void UpdateGoalEnemyWithHysteresis(float time)
        {
            var currentGoalEnemy = BotOwner.Memory.GoalEnemy;
            var currentGoalId = currentGoalEnemy?.Person?.ProfileId;
            if (currentGoalId != _lastAcceptedGoalEnemyId)
            {
                _lastAcceptedGoalEnemyId = currentGoalId;
                _lastAcceptedGoalSetTime = time;
            }

            if (BotOwner.Memory.DangerData.HaveCloseDanger)
            {
                BotOwner.Memory.GoalEnemy = null;
                return;
            }

            var candidateEnemy = BotOwner.EnemyChooser.FindDangerEnemy();
            if (candidateEnemy == null)
            {
                if (BotOwner.Memory.GoalEnemy == null && BotOwner.Memory.HaveGoal)
                {
                    BotOwner.Memory.GoalTarget.Clear();
                }

                if (currentGoalEnemy == null)
                {
                    TrySelectNearestLeadVisibleEnemy(time);
                }
                return;
            }

            if (currentGoalEnemy == null || currentGoalEnemy == candidateEnemy)
            {
                AcceptGoalEnemy(candidateEnemy, time);
                return;
            }

            var currentPerson = currentGoalEnemy.Person;
            var currentAlive = currentPerson != null && currentPerson.HealthController != null && currentPerson.HealthController.IsAlive;
            var currentStillViable = currentAlive && currentGoalEnemy.IsVisible && currentGoalEnemy.CanShoot;

            if (currentAlive && IsLeadThreatEnemy(currentPerson) && !IsLeadThreatEnemy(candidateEnemy.Person))
            {
                return;
            }

            if (!currentStillViable)
            {
                AcceptGoalEnemy(candidateEnemy, time);
                return;
            }

            if (IsLeadThreatEnemy(candidateEnemy.Person))
            {
                AcceptGoalEnemy(candidateEnemy, time);
                return;
            }

            if (currentPerson != null && currentPerson.ProfileId == _lastAcceptedGoalEnemyId && time - _lastAcceptedGoalSetTime < GOAL_SWITCH_LOCK_TIME)
            {
                return;
            }

            if (candidateEnemy.IsVisible && candidateEnemy.CanShoot
                && candidateEnemy.Distance <= currentGoalEnemy.Distance * GOAL_SWITCH_ADVANTAGE)
            {
                AcceptGoalEnemy(candidateEnemy, time);
                return;
            }

        }

        private void AcceptGoalEnemy(EnemyInfo goalEnemy, float time)
        {
            BotOwner.Memory.GoalEnemy = goalEnemy;
            _lastAcceptedGoalEnemyId = goalEnemy?.Person?.ProfileId;
            _lastAcceptedGoalSetTime = time;
        }

        private bool ShallRushEnemy(float enemyToLeadSqrDist, EnemyInfo goalEnemy, bool hasTravelTask, float botToEnemySqrDist = float.MaxValue)
        {
            if (McsBotPlayerData.IsHeavySuppressed)
            {
                return false;
            }

            if (McsBotPlayerData.HasAnyIntent(Intents.ShouldKeepFormation, Intents.ShouldUseStationaryWeapon, Intents.ShouldHoldPosition))
            {
                return false;
            }

            if (!hasTravelTask && (enemyToLeadSqrDist <= (50f * 50f) || botToEnemySqrDist <= RUSH_NEAR_ENEMY_SQUARE_DIST))
            {
                return true;
            }

            var enemyBotOwner = goalEnemy.Person?.AIData?.BotOwner;
            if (enemyBotOwner != null && enemyBotOwner.BotState != EBotState.NonActive)
            {
                if (enemyBotOwner.WeaponManager?.Reload?.Reloading == true || enemyBotOwner.Medecine?.Using == true)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsShotByEnemyRecently(EnemyInfo goalEnemy, float time)
        {
            var shooter = McsBotPlayerData.LastHitShooter;
            if (shooter == null || goalEnemy.Person == null)
            {
                return false;
            }
            return shooter.ProfileId == goalEnemy.Person.ProfileId && time - McsBotPlayerData.LastHitTime <= 6f;
        }



        public override void InitActionMap()
        {
            base.InitActionMap();
            RegisterAction(typeof(McsDogFightLogic), EndDogFight);
            RegisterAction(typeof(McsSuppressFireLogic), EndSuppressFire);
            RegisterAction(typeof(McsCoverPeakLogic), EndCoverPeak);
            RegisterAction(typeof(McsStandAndShootLogic), EndShootFromPlace);
        }

        private bool EndDogFight()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null || goalEnemy.Person == null || !goalEnemy.Person.HealthController.IsAlive)
            {
                return true;
            }

            if (BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position) > (20f * 20f))
            {
                return true;
            }

            if (!goalEnemy.IsVisible && Time.time - GetEnemyLastSeenTime() > 8f)
            {
                return true;
            }

            return false;
        }

        private bool EndSuppressFire()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null || goalEnemy.Person == null)
            {
                return true;
            }

            if (goalEnemy.IsVisible)
            {
                return true;
            }

            if (!BotOwner.WeaponManager.HaveBullets)
            {
                return true;
            }

            var time = Time.time;
            var timeSinceSeen = time - GetEnemyLastSeenTime();
            if (timeSinceSeen > SUPPRESS_TIME_SINCE_SEEN && !IsShotByEnemyRecently(goalEnemy, time))
            {
                return true;
            }

            return false;
        }

        private bool EndCoverPeak()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null || goalEnemy.Person == null || !goalEnemy.Person.HealthController.IsAlive)
            {
                return true;
            }

            if (!BotOwner.Memory.IsInCover)
            {
                return true;
            }

            return false;
        }


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

            if (mcsBotPlayerData != null && mcsBotPlayerData.HasIntent(Intents.ShouldUseStationaryWeapon))
            {
                return true;
            }

            if (BotOwner.Memory.HaveEnemy)
            {
                var goalEnemy = BotOwner.Memory.GoalEnemy;
                var enemyExist = goalEnemy != null && goalEnemy.Person != null;
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
                    if (mcsBotPlayerData?.McsAILeadPlayer?.GetLeadThreatEnemy() != null)
                    {
                        _deferToSain = false;
                        return true;
                    }

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
