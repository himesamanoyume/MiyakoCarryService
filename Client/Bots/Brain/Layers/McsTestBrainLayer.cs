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
    /// <summary>
    /// 试验层：借鉴 SAIN 优秀战斗行为的护航战斗拟人化改造。
    /// 复制自 McsBrainLayer，优先级 201，仅战斗区决策被重写（McsT: 前缀 label），
    /// 其余区域行为与原 McsBrainLayer 保持一致。
    /// DEBUG 开关 EnableTestBrainLayer 关闭时 IsActive 返回 false，自动回退 McsBrainLayer(200)，便于前后对比。
    /// </summary>
    public class McsTestBrainLayer : McsBaseLayer
    {
        public McsTestBrainLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
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

        #region 拟人化战斗常量与状态（参数出处：SAIN EnemyDecisionClass / DogFight / SeekCoverAction）

        /// <summary>
        /// 站桩窗口基准时长（秒），乘随机系数得到本次交火的站打上限（SAIN HoldGroundBaseTime=1f，护航场景放宽到 4f）
        /// </summary>
        private const float HOLD_GROUND_BASE_TIME = 4f;
        private const float HOLD_GROUND_RANDOM_MIN = 0.66f;
        private const float HOLD_GROUND_RANDOM_MAX = 1.5f;

        /// <summary>
        /// 狗斗进入/退出路径距离（米）（SAIN DOGFIGHT_PATH_DIST_START=10 / END=15；进入放宽到 12m 覆盖边缘距离）
        /// </summary>
        private const float DOGFIGHT_ENTER_SQUARE_DIST = 15f * 15f;
        private const float DOGFIGHT_EXIT_SQUARE_DIST = 20f * 20f;

        /// <summary>
        /// 狗斗进入条件：刚见敌（秒）或刚被打（秒）（SAIN DOGFIGHT_TIMESINCESEEN_START=1 + ShotMeRecently）
        /// </summary>
        private const float DOGFIGHT_VISIBLE_WINDOW = 1f;
        private const float DOGFIGHT_HIT_WINDOW = 2f;

        /// <summary>
        /// 狗斗退出：失联时长（秒）（SAIN DOGFIGHT_TIMESINCESEEN_END=8）
        /// </summary>
        private const float DOGFIGHT_UNSEEN_EXIT = 8f;

        /// <summary>
        /// 压制射击：敌人最后可见窗口（秒）（SAIN TimeSinceSeenToSuppress=3）
        /// </summary>
        private const float SUPPRESS_TIME_SINCE_SEEN = 3f;

        /// <summary>
        /// 压制射击：被该敌打过后的反压制窗口（秒）（SAIN TimeSinceShotAtToSuppress=12）
        /// </summary>
        private const float SUPPRESS_SHOT_AT_WINDOW = 6f;

        /// <summary>
        /// 保护冲脸：敌人距队长此距离内可冲（恢复旧层 50m 语义）
        /// </summary>
        private const float RUSH_PROTECT_LEAD_SQUARE_DIST = 50f * 50f;

        /// <summary>
        /// 近敌冲脸：敌人距自身此距离内可直接冲（与保护冲满足其一即触发）
        /// </summary>
        private const float RUSH_NEAR_ENEMY_SQUARE_DIST = 15f * 15f;

        /// <summary>
        /// 近距可见敌推进：bot 距敌此距离内且可见时边打边冲，不受 HoldGround 站打窗口限制
        /// </summary>
        private const float NEAR_ENEMY_ADVANCE_SQUARE_DIST = 20f * 20f;

        /// <summary>
        /// 寻掩体：自身与掩体点允许的最大移动距离（米）
        /// </summary>
        private const float SEEK_COVER_MAX_MOVE_SQUARE_DIST = 30f * 30f;

        /// <summary>
        /// 换掩体（ShiftCover）：在掩体内驻留最短时长（秒）（SAIN ShiftCoverChangeDecisionTime=6）
        /// </summary>
        private const float SHIFT_COVER_MIN_TIME = 6f;

        /// <summary>
        /// 换掩体：敌人最后一次现身距今超过此值才考虑挪窝（秒）（SAIN ShiftCoverTimeSinceSeen=30）
        /// </summary>
        private const float SHIFT_COVER_ENEMY_UNSEEN_TIME = 30f;

        /// <summary>
        /// 受击追溯选敌窗口（秒）
        /// </summary>
        private const float HIT_SELECT_WINDOW = 2f;

        /// <summary>
        /// 已抵达掩体位置的判定距离（米）
        /// </summary>
        private const float SEEK_COVER_ARRIVE_SQUARE_DIST = 1.5f * 1.5f;

        /// <summary>
        /// GoalEnemy 切换锁定窗（秒）：切换目标后此时间内不接受 CalcGoal 的再次切换
        /// （原生无滞回，靠 3.3s 低频重算防抖；MCS 战斗区 0.1s 提频重算导致多敌高频摆动，视角不停转、瞄准永远被清空）
        /// </summary>
        private const float GOAL_SWITCH_LOCK_TIME = 2f;

        /// <summary>
        /// GoalEnemy 切换优势门限：新敌需比当前敌近此比例以上（0.75=近25%）才允许切换
        /// </summary>
        private const float GOAL_SWITCH_ADVANTAGE = 0.75f;

        private string _holdGroundEnemyId = null;
        private float _holdGroundStartTime = 0f;
        private float _holdGroundDuration = 0f;
        private float _lastEnemyVisibleTime = -999f;
        private float _coverEnterTime = 0f;

        /// <summary>
        /// 滞回锁定记录：当前已接受目标的 ProfileId 与接受时刻（外部写入源换目标时视为重新接受）
        /// </summary>
        private string _lastAcceptedGoalEnemyId = null;
        private float _lastAcceptedGoalSetTime = -999f;

        #endregion

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
                #region McsTestFightLayer

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
                        return new Action(typeof(HoldPositionLogic), "McsT:FightNoEnemy");
                    }

                    var haveBullets = BotOwner?.WeaponManager?.HaveBullets;

                    TrackCoverEnter(time);

                    // 换掩体（ShiftCover）：在掩体内驻留过久且敌人长时间未现身 → 挪窝换更高掩体（借鉴 SAIN ShiftCoverAction）
                    if (_coverEnterTime > 0f
                        && time - _coverEnterTime > SHIFT_COVER_MIN_TIME
                        && time - GetEnemyLastSeenTime() > SHIFT_COVER_ENEMY_UNSEEN_TIME
                        && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation))
                    {
                        if (TryGetCombatCoverTarget(mcsLeadPlayerPos, out var shiftCoverPos))
                        {
                            UpdateCommonMoveTarget(shiftCoverPos, out var shiftNextTime);
                            if (_currentMoveTarget.HasValue)
                            {
                                _coverEnterTime = 0f;
                                ApplyMovePoint();
                                return new Action(typeof(GoToPointLogic), "McsT:ShiftCover");
                            }
                        }
                        else
                        {
                            // 没有可换的掩体：再等一个周期，避免每帧重试
                            _coverEnterTime = time - SHIFT_COVER_MIN_TIME + 5f;
                        }
                    }

                    // 已在掩体且能对敌射击 → 探头节奏 + 盲射（借鉴 SAIN SeekCoverAction / BlindFireController）
                    if (haveBullets.Value && IsShootFromCoverConditionAllFine())
                    {
                        return new Action(typeof(McsCoverPeakLogic), "McsT:CoverPeak");
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
                        return new Action(typeof(MeleeAttackLogic), "McsT:MeleeAttack");
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

                    // 压制值惰性衰减 + 受击追溯选敌（借鉴 SAIN 受击链路：2s 内被打优先把打我者设为 GoalEnemy）
                    McsBotPlayerData.UpdateSuppressionDecay();
                    TrySelectLastHitShooter(time);

                    UpdateCoverToShoot();

                    if (!goalEnemy.IsVisible && BotOwner.SmokeGrenade.ShallShoot() && BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position) <= 40f * 40f)
                    {
                        return new Action(typeof(ShootToSmokeLogic), "McsT:SmokeGrenad");
                    }
                    else
                    {
                        if (mcsLeadPlayerPos == null)
                        {
                            return new Action(typeof(HoldPositionLogic), "McsT:Uninitialized");
                        }

                        var botToEnemySqrDist = BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position);
                        var enemyToLeadSqrDist = mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position);

                        if (goalEnemy.IsVisible)
                        {
                            _lastEnemyVisibleTime = time;
                        }

                        // 狗斗：敌可见且近身（路径≈直线 ≤10m）且刚见敌(1s)/刚被打(2s) → 近身走射缠斗（借鉴 SAIN DogFight）
                        if (haveBullets.Value
                            && botToEnemySqrDist <= DOGFIGHT_ENTER_SQUARE_DIST
                            && (time - _lastEnemyVisibleTime <= DOGFIGHT_VISIBLE_WINDOW || time - McsBotPlayerData.LastHitTime <= DOGFIGHT_HIT_WINDOW)
                            && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation, Intents.ShouldUseStationaryWeapon))
                        {
                            return new Action(typeof(McsDogFightLogic), "McsT:DogFight");
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

                                // 近距可见敌推进：bot 距敌 ≤20m 且可见时边打边冲（原版 AttackMoving 行为），不受 HoldGround 站打窗口限制
                                if (botToEnemySqrDist <= NEAR_ENEMY_ADVANCE_SQUARE_DIST
                                    && !McsBotPlayerData.IsHeavySuppressed
                                    && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation)
                                    && !BotOwner.GoToSomePointData.IsCome())
                                {
                                    return new Action(typeof(AttackMovingLogic), "McsT:AttackMovingClose");
                                }

                                // HoldGround 暴露计时器：站打窗口耗尽且敌人正注视我 → 撤往掩体（借鉴 SAIN shallStandAndShoot）
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
                                        return new Action(typeof(GoToPointLogic), heavySuppressed ? "McsT:SuppressedSeekCover" : "McsT:SeekCover");
                                    }
                                }

                                if (!BotOwner.GoToSomePointData.IsCome() && !McsBotPlayerData.HasAnyIntent(Intents.ShouldHoldPosition, Intents.ShouldFollowMe, Intents.ShouldKeepFormation))
                                {
                                    return new Action(typeof(AttackMovingLogic), "McsT:AttackMoving");
                                }
                                else
                                {
                                    // 站桩射击 + 概率微走位（借鉴 SAIN StandAndShootAction.moveShoot）
                                    return new Action(typeof(McsStandAndShootLogic), "McsT:StandAndShoot");
                                }
                            }
                            else
                            {
                                // 敌不可见：重置站打计时
                                _holdGroundEnemyId = null;

                                // 近敌听声清剿：bot 距敌 ≤15m 直接冲（优先于压制射击），近距保持进攻性
                                if (botToEnemySqrDist <= RUSH_NEAR_ENEMY_SQUARE_DIST && ShallRushEnemy(enemyToLeadSqrDist, goalEnemy, hasTravelTask))
                                {
                                    return new Action(typeof(RunToEnemyLogic), "McsT:RushEnemyNear");
                                }

                                // 压制射击：刚丢敌（≤3s）或被该敌打过（≤6s）→ 朝最后已知点开火（借鉴 SAIN 反压制），替代无脑冲脸
                                var timeSinceSeen = time - GetEnemyLastSeenTime();
                                var shotByThisEnemy = IsShotByEnemyRecently(goalEnemy, time);
                                if (timeSinceSeen <= SUPPRESS_TIME_SINCE_SEEN || shotByThisEnemy)
                                {
                                    return new Action(typeof(McsSuppressFireLogic), "McsT:SuppressFire");
                                }

                                // 保护冲脸：敌距队长 ≤50m（恢复旧层语义）或敌脆弱（换弹/治疗中）才冲
                                if (ShallRushEnemy(enemyToLeadSqrDist, goalEnemy, hasTravelTask))
                                {
                                    return new Action(typeof(RunToEnemyLogic), "McsT:RushEnemy");
                                }

                                if (McsBotPlayerData.HasIntent(Intents.ShouldGoToPoint))
                                {
                                    if (TryRefreshCommonTarget(McsBotPlayerData.TargetPos, time))
                                    {
                                        ApplyMovePoint();
                                        return new Action(typeof(GoToPointLogic), "McsT:GoToPointCommand");
                                    }
                                    else
                                    {
                                        return new Action(typeof(HoldPositionLogic), "McsT:GoToLootTargetPosNotFound");
                                    }
                                }

                                if (McsBotPlayerData.HasIntent(Intents.ShouldHoldPosition))
                                {
                                    if (needHeal && isEnemyPosLost)
                                    {
                                        RefreshStuckTimer();
                                        return new Action(typeof(HealLogic), "McsT:FightHealing4");
                                    }
                                    return new Action(typeof(HoldPositionLogic), "McsT:HoldPositionCommand");
                                }

                                TryRefreshLeadTarget(mcsLeadPlayerPos, time);

                                if (needHeal && isEnemyPosLost)
                                {
                                    RefreshStuckTimer();
                                    ApplyMovePoint();
                                    return new Action(typeof(HealLogic), "McsT:FightHealing5");
                                }

                                if (sqrDistance >= TOO_FAR_FROM_LEAD_DISTANCE * 1 || tooClose)
                                {
                                    if (_currentMoveTarget.HasValue)
                                    {
                                        ApplyMovePoint();
                                        return new Action(typeof(GoToPointLogic), tooClose ? "McsT:TooClose" : "McsT:TooFar");
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
                                            return new Action(typeof(GoToPointLogic), "McsT:Partoling");
                                        }
                                    }
                                    else
                                    {
                                        if (needHeal && isEnemyPosLost)
                                        {
                                            RefreshStuckTimer();
                                            return new Action(typeof(HealLogic), "McsT:FightHealing6");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // !safeFire || !haveBullets：没子弹 → 优先掩体方向换弹，不再无脑冲脸
                            if (!haveBullets.Value && TryGetCombatCoverTarget(mcsLeadPlayerPos, out var reloadCoverPos))
                            {
                                UpdateCommonMoveTarget(reloadCoverPos, out var reloadNextTime);
                                if (_currentMoveTarget.HasValue)
                                {
                                    ApplyMovePoint();
                                    return new Action(typeof(GoToPointLogic), "McsT:ReloadInCover");
                                }
                            }

                            if (ShallRushEnemy(enemyToLeadSqrDist, goalEnemy, hasTravelTask, botToEnemySqrDist))
                            {
                                return new Action(typeof(RunToEnemyLogic), "McsT:RushEnemy");
                            }

                            if (McsBotPlayerData.HasIntent(Intents.ShouldGoToPoint))
                            {
                                if (TryRefreshCommonTarget(McsBotPlayerData.TargetPos, time))
                                {
                                    ApplyMovePoint();
                                    return new Action(typeof(GoToPointLogic), "McsT:GoToPointCommand");
                                }
                                else
                                {
                                    return new Action(typeof(HoldPositionLogic), "McsT:GoToLootTargetPosNotFound");
                                }
                            }

                            if (McsBotPlayerData.HasIntent(Intents.ShouldHoldPosition))
                            {
                                if (needHeal && isEnemyPosLost)
                                {
                                    RefreshStuckTimer();
                                    return new Action(typeof(HealLogic), "McsT:FightHealing4");
                                }
                                return new Action(typeof(HoldPositionLogic), "McsT:HoldPositionCommand");
                            }

                            TryRefreshLeadTarget(mcsLeadPlayerPos, time);

                            if (needHeal && isEnemyPosLost)
                            {
                                RefreshStuckTimer();
                                ApplyMovePoint();
                                return new Action(typeof(HealLogic), "McsT:FightHealing5");
                            }

                            if (sqrDistance >= TOO_FAR_FROM_LEAD_DISTANCE * 1 || tooClose)
                            {
                                if (_currentMoveTarget.HasValue)
                                {
                                    ApplyMovePoint();
                                    return new Action(typeof(GoToPointLogic), tooClose ? "McsT:TooClose" : "McsT:TooFar");
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
                                        return new Action(typeof(GoToPointLogic), "McsT:Partoling");
                                    }
                                }
                                else
                                {
                                    if (needHeal && isEnemyPosLost)
                                    {
                                        RefreshStuckTimer();
                                        return new Action(typeof(HealLogic), "McsT:FightHealing6");
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

        #region 拟人化战斗辅助方法

        /// <summary>
        /// 站打计时：敌人切换或重新可见时重置窗口（借鉴 SAIN CalcHoldGroundDelay）
        /// </summary>
        private void UpdateHoldGroundTimer(EnemyInfo goalEnemy, float time)
        {
            var enemyId = goalEnemy.Person?.ProfileId;
            if (_holdGroundEnemyId != enemyId)
            {
                _holdGroundEnemyId = enemyId;
                _holdGroundStartTime = time;
                _holdGroundDuration = HOLD_GROUND_BASE_TIME * UnityEngine.Random.Range(HOLD_GROUND_RANDOM_MIN, HOLD_GROUND_RANDOM_MAX);
            }
        }

        /// <summary>
        /// 敌人是否正注视我（bot 敌人查其 GoalEnemy；玩家敌人保守认为正注视，用于触发站打计时）
        /// </summary>
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

        /// <summary>
        /// 受击追溯选敌：2s 内被打且打人者存活时，把打我者设为主目标（借鉴 SAIN GetHit 链路）
        /// </summary>
        private void TrySelectLastHitShooter(float time)
        {
            var shooter = McsBotPlayerData.LastHitShooter;
            if (shooter == null || time - McsBotPlayerData.LastHitTime > HIT_SELECT_WINDOW || !shooter.HealthController.IsAlive)
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

        /// <summary>
        /// 战斗掩体目标：优先用已刷新的可射击掩体点，其次查 hide 型掩体；限制距自身与队长的距离，已在位则返回 false
        /// </summary>
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

            if (BotOwner.Position.McsSqrDistance(coverPoint.Position) > SEEK_COVER_MAX_MOVE_SQUARE_DIST)
            {
                return false;
            }

            if (mcsLeadPlayerPos.McsSqrDistance(coverPoint.Position) > TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
            {
                return false;
            }

            if (BotOwner.Position.McsSqrDistance(coverPoint.Position) <= SEEK_COVER_ARRIVE_SQUARE_DIST)
            {
                return false;
            }

            coverPos = coverPoint.Position;
            return true;
        }

        /// <summary>
        /// 记录进入掩体的时刻（换掩体判定用）
        /// </summary>
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

        /// <summary>
        /// 带滞回的目标重算（多敌防抖）：不调 BotOwner.CalcGoal（它会无条件写 GoalEnemy），
        /// 改用 EnemyChooser.FindDangerEnemy 只读预判最优敌，通过滞回判定才写入 Memory——
        /// 拒绝切换时 setter 不触发、瞄准不被清空（原生防抖靠 3.3s 低频重算，MCS 0.1s 提频后必须自带滞回）。
        /// 规则：当前敌不可见/不可射/死亡时无条件接受新目标；否则需过锁定窗（GOAL_SWITCH_LOCK_TIME）
        /// 且新敌有距离优势（近 GOAL_SWITCH_ADVANTAGE 比例）。受击追溯选敌在滞回之前执行，不受影响。
        /// </summary>
        private void UpdateGoalEnemyWithHysteresis(float time)
        {
            // 外部写入源（受击追溯/队长广播）可能已换目标：同步锁定记录（视为重新接受，锁定窗从现在起算）
            var currentGoalEnemy = BotOwner.Memory.GoalEnemy;
            var currentGoalId = currentGoalEnemy?.Person?.ProfileId;
            if (currentGoalId != _lastAcceptedGoalEnemyId)
            {
                _lastAcceptedGoalEnemyId = currentGoalId;
                _lastAcceptedGoalSetTime = time;
            }

            // 近身危险时对齐原生语义：清目标（原生 CalcGoalForBot 的 HaveCloseDanger 分支）
            if (BotOwner.Memory.DangerData.HaveCloseDanger)
            {
                BotOwner.Memory.GoalEnemy = null;
                return;
            }

            var candidateEnemy = BotOwner.EnemyChooser.FindDangerEnemy();
            if (candidateEnemy == null)
            {
                // 无可锁目标：对齐原生语义只在完全无目标时清空（保留丢敌后的压制射击等状态依赖）
                if (BotOwner.Memory.GoalEnemy == null && BotOwner.Memory.HaveGoal)
                {
                    BotOwner.Memory.GoalTarget.Clear();
                }
                return;
            }

            // 无当前目标或候选与当前相同 → 直接接受（首次锁定无滞回）
            if (currentGoalEnemy == null || currentGoalEnemy == candidateEnemy)
            {
                AcceptGoalEnemy(candidateEnemy, time);
                return;
            }

            var currentPerson = currentGoalEnemy.Person;
            var currentAlive = currentPerson != null && currentPerson.HealthController != null && currentPerson.HealthController.IsAlive;
            var currentStillViable = currentAlive && currentGoalEnemy.IsVisible && currentGoalEnemy.CanShoot;

            // 当前敌已失效（死亡/不可见/不可射）→ 接受新目标
            if (!currentStillViable)
            {
                AcceptGoalEnemy(candidateEnemy, time);
                return;
            }

            // 锁定窗内：不接受切换
            if (currentPerson != null && currentPerson.ProfileId == _lastAcceptedGoalEnemyId && time - _lastAcceptedGoalSetTime < GOAL_SWITCH_LOCK_TIME)
            {
                return;
            }

            // 锁定窗外：新敌须有距离优势（近 25% 以上）才允许切换
            if (candidateEnemy.IsVisible && candidateEnemy.CanShoot
                && candidateEnemy.Distance <= currentGoalEnemy.Distance * GOAL_SWITCH_ADVANTAGE)
            {
                AcceptGoalEnemy(candidateEnemy, time);
                return;
            }

            // 拒绝切换：保持当前目标（不写 Memory，setter 不触发，瞄准保留）
        }

        /// <summary>
        /// 接受新目标：写入 Memory 并刷新滞回锁定记录
        /// </summary>
        private void AcceptGoalEnemy(EnemyInfo goalEnemy, float time)
        {
            BotOwner.Memory.GoalEnemy = goalEnemy;
            _lastAcceptedGoalEnemyId = goalEnemy?.Person?.ProfileId;
            _lastAcceptedGoalSetTime = time;
        }

        /// <summary>
        /// 冲脸条件：敌距队长 ≤50m（保护，恢复旧层语义）或 bot 距敌 ≤15m 或敌脆弱（bot 敌人换弹/治疗中），
        /// 且非重度压制、无阵位指令
        /// </summary>
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

            if (!hasTravelTask && (enemyToLeadSqrDist <= RUSH_PROTECT_LEAD_SQUARE_DIST || botToEnemySqrDist <= RUSH_NEAR_ENEMY_SQUARE_DIST))
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

        /// <summary>
        /// 最近打到我的是否为当前敌人（压制射击的反压制窗口判定）
        /// </summary>
        private bool IsShotByEnemyRecently(EnemyInfo goalEnemy, float time)
        {
            var shooter = McsBotPlayerData.LastHitShooter;
            if (shooter == null || goalEnemy.Person == null)
            {
                return false;
            }
            return shooter.ProfileId == goalEnemy.Person.ProfileId && time - McsBotPlayerData.LastHitTime <= SUPPRESS_SHOT_AT_WINDOW;
        }

        #endregion

        #region 新 Logic 结束条件

        public override void InitActionMap()
        {
            base.InitActionMap();
            RegisterAction(typeof(McsDogFightLogic), EndDogFight);
            RegisterAction(typeof(McsSuppressFireLogic), EndSuppressFire);
            RegisterAction(typeof(McsCoverPeakLogic), EndCoverPeak);
            RegisterAction(typeof(McsStandAndShootLogic), EndShootFromPlace);
        }

        public bool EndDogFight()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null || goalEnemy.Person == null || !goalEnemy.Person.HealthController.IsAlive)
            {
                return true;
            }

            if (BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position) > DOGFIGHT_EXIT_SQUARE_DIST)
            {
                return true;
            }

            if (!goalEnemy.IsVisible && Time.time - GetEnemyLastSeenTime() > DOGFIGHT_UNSEEN_EXIT)
            {
                return true;
            }

            return false;
        }

        public bool EndSuppressFire()
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

        public bool EndCoverPeak()
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

        #endregion

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
            if (!MiyakoCarryServicePlugin.EnableTestBrainLayer.Value)
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
