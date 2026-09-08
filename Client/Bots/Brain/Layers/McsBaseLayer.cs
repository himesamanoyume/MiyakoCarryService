using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Vehicle;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public abstract class McsBaseLayer : CustomLayer
    {
        public McsBaseLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            InitActionMap();
        }

        public bool? _isMcsBotPlayer = null;
        public bool IsMcsBotPlayer => _isMcsBotPlayer ??= BotOwner.IsMcsBotPlayer;
        protected ConcurrentDictionary<Type, Func<bool>> _endActionMap;
        public bool _haveCoverToShoot = false;
        public float _nextHoldPositionTime = 0f;
        public float _goToCoverTime = 0f;
        public CustomNavigationPoint _currentNavigationPoint = null;
        public float _nextPatrolTime = 0f;
        public float _nextShootTime = 0f;
        public float _nextWeaponSwitchTime = 0f;
        public float _nextMeleeCheckTime = 0f;
        public float _nextLootingCheckTime = 0f;
        public float _nextVaultCheckTime = 0f;
        public float _nextUpdatePosTime = 0f;
        public float _nextHealCheckTime = 0f;
        public float _nextStimCheckTime = 0f;
        public float _nextDeactivateCheckTime = 0f;
        public Vector3? _currentMoveTarget = null;
        public Vector3? _lastTargetPos = Vector3.zero;
        public Vector3[] _lastCalcCorners = null;
        public bool _lastCanRunResult = false;
        public int _currentMoveRetries = 0;
        public int _currentHealTimes = 0;
        public int _currentLootingRetries = 0;
        public int _currentDeactivateRetries = 0;
        public float _currentHealTimeout = 10f;
        public float _nextAnimatorFixTime = 0f;
        public string _cachedProxyTargetId;
        public StationaryWeaponData _cachedStationaryWeaponData;
        public float _scanPhase = 0f;
        public const float SCANPERIOD = 4f;
        public const float SCANDISTANCE = 30f;
        public const float SCANPITCHDOWN = 2f;
        public const float LEAD_POSITION_CHANGE_THRESHOLD = 2f;
        public const float TOO_FAR_FROM_LEAD_DISTANCE = 20f;
        public const float TOO_CLOSE_FROM_LEAD_DISTANCE = 2f;
        public const float HEAL_CHECK_INTERVAL = 1f;
        public const float VAULT_CHECK_INTERVAL = 2f;
        public const float VAULT_HEIGHT_THRESHOLD = 1.5f;
        public const float SPHERECAST_RADIUS = 0.1f;
        public const float SPHERECAST_DISTANCE = 2f;
        public const float DIRECTION_ALIGNMENT_THRESHOLD = 0.85f;
        public const float ENTER_COMMON_LOOTING_COODDOWN = 10f;
        public const float LOOTING_FINNISHED_COODDOWN = 1f;
        public const float WEAPON_SWITCH_COOLDOWN = 1f;
        public const float MELEE_CHECK_INTERVAL = 0.5f;

        /// <summary>
        /// 威胁中断-受击窗口（秒）：窗口内被打过视为威胁逼近（与原生 CheckMedsToStop 信号同源；
        /// 不依赖 GoalEnemy——被伏击时先开战斗闸门，滞回/受击追溯随后选目标）
        /// </summary>
        public const float THREAT_INTERRUPT_HIT_WINDOW = 2f;

        /// <summary>
        /// 威胁中断-近身距离（米）：GoalEnemy 距自身此距离内视为威胁逼近（原生 CheckMedsToStop 同款 10m）
        /// </summary>
        public const float THREAT_INTERRUPT_CLOSE_DIST = 10f;

        /// <summary>
        /// 威胁中断-近距威胁距离（米）：GoalEnemy 距自身此距离内且可见/正盯我时视为威胁逼近（原生同款 30m）
        /// </summary>
        public const float THREAT_INTERRUPT_NEAR_DIST = 30f;

        /// <summary>
        /// 威胁中断-被盯判定时长（秒）：敌盯我此时长视为威胁（原生 IsEnemyLookAtMeForPeriod 同参数）
        /// </summary>
        public const float THREAT_INTERRUPT_LOOK_PERIOD = 2f;

        /// <summary>
        /// 快速开门-路径检测距离（米）：门洞中点距自身此距离内才做路径求交检测（原生 wantPass 2m 的前瞻放宽）
        /// </summary>
        public const float FAST_OPEN_DOOR_PATH_CHECK_DIST = 12f;

        /// <summary>
        /// 快速开门-碰撞忽略窗口（秒）：指令直开后无视门碰撞的时长（覆盖门摆动 ~0.5-2s + 通过余量；
        /// SAIN 用 1.0/1.25s 交互窗口，此值偏保守防半开门碰撞）
        /// </summary>
        public const float FAST_OPEN_DOOR_WINDOW = 2.5f;

        /// <summary>
        /// 快速开门-同门冷却（秒）：同一扇门两次快速开门的最小间隔（SAIN DOOR_INTERACTION_INTERVAL 同款），防反复开关抖动
        /// </summary>
        public const float FAST_OPEN_DOOR_COOLDOWN = 3.5f;

        /// <summary>
        /// 快速开门-路径求交前段长度（米）：只检测路径累计此长度内的段（与移动目标取点距离 15m 对齐）
        /// </summary>
        public const float FAST_OPEN_DOOR_PATH_LOOKAHEAD = 15f;

        /// <summary>
        /// 快速开门-楼层防护高差（米）：路径段与门洞中点高差超过此值视为跨层，不做开门判定
        /// </summary>
        public const float FAST_OPEN_DOOR_FLOOR_HEIGHT_DIFF = 1.5f;

        public McsBotPlayerData McsBotPlayerData
        {
            get
            {
                return field ??= BotOwner.GetMcsBotPlayerData();
            }
        }

        public string Name
        {
            get
            {
                return field ??= GetType().Name;
            }
        }

        public override string GetName()
        {
            return Name;
        }

        public override void Start()
        {
            base.Start();
            if (MiyakoCarryServicePlugin.SAINInstalled)
            {
                // 如果不执行这段代码，当护航从SAIN的Layer回到Mcs的Layer时，就会卡住不动（以前会，现在似乎删除也不会再发生了，但避免意外，依旧保留）
                SAINUtils.ResetSAINLayer(BotOwner);
            }
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.IsMcsLayerActive = true;
            }
        }

        public override void Stop()
        {
            base.Stop();
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.IsMcsLayerActive = false;
            }
        }

        protected SubtitlesMgr SubtitlesMgr => field ??= MgrAccessor.Get<SubtitlesMgr>();

        public override bool IsCurrentActionEnding()
        {
            if (CurrentAction == null)
            {
                return true;
            }

            return _endActionMap.TryGetValue(CurrentAction.Type, out var endFunc) ? endFunc() : true;
        }

        public void RegisterAction(Type logicType, Func<bool> func)
        {
            if (_endActionMap == null)
            {
                _endActionMap = new();
            }

            _endActionMap.AddOrUpdate(logicType, _logicType => func,
                (_logicType, oldFunc) =>
                {
                    oldFunc = func;
                    return oldFunc;
                }
            );
        }

        public virtual void InitActionMap()
        {
            RegisterAction(typeof(GoToCoverPointLogic), EndGoToCoverPoint);
            RegisterAction(typeof(HealLogic), EndHeal);
            RegisterAction(typeof(StationaryHealLogic), EndHeal);
            RegisterAction(typeof(RunToCoverLogic), EndRunToCover);
            RegisterAction(typeof(SimplePatrolLogic), EndSimplePatrol);
            RegisterAction(typeof(HoldPositionLogic), EndHoldPosition);
            RegisterAction(typeof(GoToPointLogic), EndGoToPoint);
            RegisterAction(typeof(GoToProtectLogic), EndGoToProtect);
            RegisterAction(typeof(GoToEnemyLogic), EndGoToEnemy);
            RegisterAction(typeof(AttackMovingLogic), EndAttackMoving);
            RegisterAction(typeof(GoToLootTargetLogic), EndGoToLootTarget);
            RegisterAction(typeof(ShootFromPlaceLogic), EndShootFromPlace);
            RegisterAction(typeof(ShootFromCoverLogic), EndShootFromCover);
            RegisterAction(typeof(ShootToSmokeLogic), EndShootToSmoke);
            RegisterAction(typeof(ShootFromStationaryLogic), EndShootFromStationary);
            RegisterAction(typeof(RunToEnemyLogic), EndRunToEnemy);
            RegisterAction(typeof(GoToExfiltrationPointLogic), EndGoToExfiltrationPoint);
            RegisterAction(typeof(MeleeAttackLogic), EndMeleeAttack);
            RegisterAction(typeof(RunToPointLogic), EndGoToPoint);
            RegisterAction(typeof(EscortToPointByWayLogic), EndEscortToPointByWay);
            RegisterAction(typeof(FlashedLogic), EndFlashed);
            RegisterAction(typeof(DeactivateMineLogic), EndDeactivateMine);
            RegisterAction(typeof(RunAwayGrenadeLogic), EndRunAwayGrenade);
            RegisterAction(typeof(RunAwayArtilleryLogic), EndRunAwayArtillery);
            RegisterAction(typeof(RunAwayBTRLogic), EndRunAwayBTR);
            RegisterAction(typeof(GoToExcuteProxyActionLogic), EndGoToExcuteProxyAction);
            RegisterAction(typeof(DropTargetLootLogic), EndDropTargetLootLogic);
            RegisterAction(typeof(HealStimulatorsLogic), EndHealStimulators);
            RegisterAction(typeof(GoToBtrLogic), EndGoToBtr);
        }

        /// <summary>
        /// 威胁逼近判定（战斗区保持与移动/任务类动作中断共用，信号与原生治疗中断 CheckMedsToStop 同源）：
        /// ① 2s 内被打（无 GoalEnemy 依赖——被伏击时先开闸，滞回/受击追溯随后选目标）；
        /// ② GoalEnemy 存活且距自身 &lt;10m（近身险情）；
        /// ③ GoalEnemy 存活且距自身 &lt;30m 且（可见/正盯我 2s）（近距对峙）；
        /// ④ 老板高威胁（GetLeadThreatEnemy，威胁窗口内攻击过/正瞄老板）——全员保持战斗，集火参与。
        /// 消费方：McsBrainLayer 的 fightActive 威胁保持，
        /// 以及基类各移动/任务类 End 的威胁中断（威胁时结束当前动作 → 重评 → 战斗区接住；
        /// 战斗类/避险类 End 不接——前者本就是应战，后者跑雷/跑炮击/闪光不可被打断）
        /// </summary>
        protected bool IsApproachingThreat()
        {
            var time = Time.time;

            // ① 受击威胁：最近被打（护航 bot 自身，ApplyDamagePatch 记录）
            var mcsBotPlayerData = McsBotPlayerData;
            if (mcsBotPlayerData != null)
            {
                var lastHitShooter = mcsBotPlayerData.LastHitShooter;
                if (lastHitShooter != null
                    && lastHitShooter.HealthController != null
                    && lastHitShooter.HealthController.IsAlive
                    && time - mcsBotPlayerData.LastHitTime <= THREAT_INTERRUPT_HIT_WINDOW)
                {
                    return true;
                }
            }

            var goalEnemy = BotOwner.Memory.GoalEnemy;

            // ②/③ 目标威胁：按距离分级判定
            if (goalEnemy != null && goalEnemy.Person != null && goalEnemy.Person.HealthController.IsAlive)
            {
                var enemyDistance = goalEnemy.Distance;
                if (enemyDistance < THREAT_INTERRUPT_CLOSE_DIST)
                {
                    return true;
                }

                if (enemyDistance < THREAT_INTERRUPT_NEAR_DIST)
                {
                    if (goalEnemy.IsVisible)
                    {
                        return true;
                    }

                    BotOwner.EnemyLookData.DoCheck();
                    if (BotOwner.EnemyLookData.IsEnemyLookAtMeForPeriod(THREAT_INTERRUPT_LOOK_PERIOD))
                    {
                        return true;
                    }
                }
            }

            // ④ 老板威胁：威胁窗口内全员保持战斗（集火参与），不再依赖自身能否看见该敌
            if (mcsBotPlayerData?.McsAILeadPlayer?.GetLeadThreatEnemy() != null)
            {
                return true;
            }

            return false;
        }

        public virtual bool EndHeal()
        {
            if (!BotOwner.Medecine.Using)
            {
                return true;
            }

            // 威胁中断（IsApproachingThreat 含被打/老板高威胁，覆盖原生 CheckMedsToStop 的 10m/30m 场景）：
            // 中断时取消当前急救（对齐原生 AssaultHaveEnemyLayer.EndHeal 语义，避免 Medecine.Using 残留）
            if (IsApproachingThreat() || BaseLogicLayer.CheckMedsToStop(BotOwner))
            {
                BotOwner.Medecine.FirstAid.CancelCurrent();
                _currentHealTimes = 0;
                return true;
            }

            if (Time.time > _nextHealCheckTime)
            {
                if (GetHealTimeout(out var timeout))
                {
                    _currentHealTimeout = timeout;
                }
                _nextHealCheckTime = Time.time + HEAL_CHECK_INTERVAL;
                _currentHealTimes += 1;
            }

            if (_currentHealTimes >= _currentHealTimeout)
            {
                BotOwner.WeaponManager.CheckWeaponReady();
                if (!CheckFirearmsAnimatorState())
                {
                    BotOwner.WeaponManager.CheckWeaponReady();
                }
                BotOwner.TryResetHandsState();
                _currentHealTimes = 0;
                return true;
            }

            return false;
        }

        public virtual bool GetHealTimeout(out float timeout)
        {
            timeout = 0f;
            if (BotOwner.Medecine.Stimulators.Using)
            {
                timeout = 3f;
                return true;
            }
            if (BotOwner.Medecine.FirstAid.Have2Do)
            {
                timeout = 10f;
                return true;
            }
            if (BotOwner.Medecine.SurgicalKit.HaveWork)
            {
                timeout = 20f;
                return true;
            }
            return false;
        }

        public virtual bool EndHealStimulators()
        {
            // 威胁中断：打兴奋剂是战术增益非生存必需，威胁逼近时中断应战
            if (IsApproachingThreat())
            {
                return true;
            }

            if (BotOwner.Medecine.Stimulators.Using)
            {
                return false;
            }
            return true;
        }

        public virtual bool EndGoToBtr()
        {
            return true;
        }

        public virtual bool EndRunToCover()
        {
            var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (mcsLeadPlayerPos != null)
            {
                TryFindCover(mcsLeadPlayerPos);
                UpdateCoverToShoot();
                if (!BotOwner.Memory.IsInCover && !_haveCoverToShoot)
                {
                    return true;
                }
            }

            if (BotOwner.Memory.IsInCover)
            {
                return true;
            }

            if (!BotOwner.CanSprintPlayer)
            {
                return true;
            }

            if (IsDogFighting())
            {
                return true;
            }

            if (BotOwner.Memory.CurCustomCoverPoint != null && BotOwner.Memory.CurCustomCoverPoint.IsSpotted)
            {
                return true;
            }

            return false;
        }

        public virtual void TryFindCover(Vector3 mcsLeadPlayerPos)
        {
            if (_goToCoverTime < Time.time)
            {
                _goToCoverTime = Time.time + 1f;
                var coverSearchData = new CoverSearchData(mcsLeadPlayerPos, BotOwner.CoverSearchInfo, CoverShootType.hide, TOO_FAR_FROM_LEAD_DISTANCE, 0f, CoverSearchType.closerToSelectedPoint, null, null, new Vector3?(mcsLeadPlayerPos), ECheckSHootHide.shootAndHide, new CoverSearchDefenceData(0f), PointsArrayType.byShootType, true, null, null, "Default");
                _currentNavigationPoint = BotOwner.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);
                if (_currentNavigationPoint != null)
                {
                    if (mcsLeadPlayerPos.McsSqrDistance(_currentNavigationPoint.Position) < TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE && !_currentNavigationPoint.IsSpotted)
                    {
                        BotOwner.Memory.IsInCover = true;
                        return;
                    }
                }
                BotOwner.Memory.IsInCover = false;
            }
        }

        public virtual void UpdateCoverToShoot()
        {
            if (McsBotPlayerData?.LeadPlayer == null)
            {
                return;
            }

            if (_nextHoldPositionTime < Time.time)
            {
                _nextHoldPositionTime = Time.time + 1f;
                Vector3 leadPos;

                if (McsBotPlayerData.LeadPlayer.HealthController == null)
                {
                    return;
                }

                if (McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
                {
                    leadPos = McsBotPlayerData.LeadPlayer.Position;
                }
                else if (BotOwner.BotFollower.HaveBoss)
                {
                    leadPos = BotOwner.BotFollower.BossToFollow.Position;
                }
                else
                {
                    leadPos = BotOwner.Position;
                }
                _currentNavigationPoint = FollowerCheckData();
                if (_currentNavigationPoint != null && _currentNavigationPoint.IsFreeById(BotOwner.Id) && !_currentNavigationPoint.IsSpotted)
                {
                    var sqrMagnitude = leadPos.McsSqrDistance(_currentNavigationPoint.Position);
                    if (sqrMagnitude >= 75f * 75f)
                    {
                        _haveCoverToShoot = false;
                        return;
                    }
                    if (ProtectCareKill())
                    {
                        _haveCoverToShoot = _currentNavigationPoint.CanIShootToEnemy;
                    }
                    else
                    {
                        _haveCoverToShoot = true;
                    }
                    if (_haveCoverToShoot && (BotOwner.Memory.CurCustomCoverPoint == null || BotOwner.Memory.CurCustomCoverPoint.Id != _currentNavigationPoint.Id))
                    {
                        BotOwner.Memory.BotCurrentCoverInfo.Spotted();
                        BotOwner.Memory.BotCurrentCoverInfo.SetCover(_currentNavigationPoint, true);
                        return;
                    }
                }
                else
                {
                    _haveCoverToShoot = false;
                }
            }
        }

        public virtual bool WasHitRecently(float timeframe)
        {
            return (Time.time - BotOwner.Memory.LastTimeHit) < timeframe;
        }

        public virtual bool EndEatDrink()
        {
            return true;
        }

        public virtual bool EndGoToCoverPoint()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (mcsLeadPlayerPos != null)
            {
                TryFindCover(mcsLeadPlayerPos);
                UpdateCoverToShoot();
                if (!BotOwner.Memory.IsInCover && !_haveCoverToShoot)
                {
                    return true;
                }
            }

            if (BotOwner.Memory.IsInCover)
            {
                return true;
            }

            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.IsVisible && goalEnemy.CanShoot)
            {
                return true;
            }

            return false;
        }

        public virtual bool EndSimplePatrol()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (ShouldEndPatrol())
            {
                return true;
            }

            if (BotOwner.PatrollingData.Way.PatrolType == PatrolType.reserved)
            {
                return true;
            }

            if (McsBotPlayerData.LeadPlayer != null && McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
            {
                return true;
            }

            if (BotOwner.BotFollower.HaveBoss && !BotOwner.Boss.IamBoss)
            {
                return true;
            }

            return false;
        }

        public virtual bool EndGoToPoint()
        {
            // 威胁中断：指定点前往途中威胁逼近时结束动作重评应战（intent/TargetPos 保留，战后自动续走）
            if (IsApproachingThreat())
            {
                return true;
            }

            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (BotOwner.GoToSomePointData.IsCome())
            {
                if (McsBotPlayerData.HasIntent(Intents.ShouldGoToPoint) && BotOwner.Position.McsSqrDistance(McsBotPlayerData.TargetPos.Value) <= 2f * 2f)
                {
                    McsBotPlayerData.SetIntent([Intents.ShouldFollowMe, Intents.ShouldKeepFormation], Intents.ShouldHoldPosition);
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.OnPosition
                    });
                }
                return true;
            }
            else
            {
                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
                if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
                {
                    CheckStuck();
                }

                if (Time.time - BotOwner.Mover._lastTimePosChanged > 30f && BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
                {
                    BotOwner.StopMove();
                    BotOwner.Mover.AllowTeleport();
                    BotOwner.GetPlayer.Teleport(McsBotPlayerData.LeadPlayer.Position, true);
                    var playerPosition = McsBotPlayerData.Player.Position;
                    BotOwner.Mover._lastGoodCastPoint = BotOwner.Mover._prevSuccessLinkedFrom = BotOwner.Mover._prevLinkPos = BotOwner.Mover.PositionOnWayInner = playerPosition;
                    BotOwner.Mover._lastGoodCastPointTime = Time.time;
                    BotOwner.Mover._prevPosLinkedTime = 0f;
                    BotOwner.Mover.SetPlayerToNavMesh(playerPosition);
                    BotOwner.Mover.RecalcWay();
                    BotOwner.Mover.Pause = true;
                    UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                    }
                    return true;
                }

                if (Time.time - BotOwner.Mover._lastTimePosChanged > 6f)
                {
                    if (McsBotPlayerData.HasIntent(Intents.ShouldGoToPoint) && BotOwner.Position.McsSqrDistance(McsBotPlayerData.TargetPos.Value) <= 2f * 2f)
                    {
                        McsBotPlayerData.SetIntent([Intents.ShouldFollowMe, Intents.ShouldKeepFormation], Intents.ShouldHoldPosition);
                        BotOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.OnPosition
                        });
                    }
                    return true;
                }
                return false;
            }
        }

        public async Task DelaySetIntents(float delaySeconds, string[] exclude = null, params string[] intents)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.SetIntent(exclude, intents);
            }
        }

        public virtual bool EndEscortToPointByWay()
        {
            // 威胁中断：跟随/护送途中威胁逼近时结束动作重评应战（长途跟随闷头走的主场景；
            // intent/TargetPos 保留，战后自动落回本分区续走）
            if (IsApproachingThreat())
            {
                return true;
            }

            if (McsBotPlayerData == null)
            {
                return true;
            }

            var hasEscortToBtr = McsBotPlayerData.HasIntent(Intents.ShouldEscortToBtr);
            var hasEscort = McsBotPlayerData.HasIntent(Intents.ShouldEscort);

            if (!hasEscort && !hasEscortToBtr)
            {
                return true;
            }

            var btrController = Singleton<GameWorld>.Instance.BtrController;
            if ((hasEscort && !McsBotPlayerData.TargetPos.HasValue) || (hasEscortToBtr && !btrController.Initiated()))
            {
                return true;
            }

            Vector3? targetPos;
            if (hasEscort)
            {
                targetPos = McsBotPlayerData.TargetPos;
            }
            else
            {
                var side = btrController.BtrView.GetBtrSide(1);
                if (side == null)
                {
                    return true;
                }
                targetPos = side.GoInPoints().Item1;
            }

            var sqrDistance = targetPos.Value.McsSqrDistance(BotOwner.Position);
            if (sqrDistance < 2f * 2f)
            {
                McsBotPlayerData.SetIntent([Intents.ShouldFollowMe, Intents.ShouldKeepFormation], Intents.ShouldHoldPosition);
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.OnPosition
                });
                TasksExtensions.HandleExceptions(DelaySetIntents(3f, [Intents.ShouldFollowMe, Intents.ShouldGoToPoint, Intents.ShouldEscort, Intents.ShouldEscortToBtr, Intents.ShouldKeepFormation]));
                return true;
            }
            else if (BotOwner.GoToSomePointData.IsCome())
            {
                return true;
            }
            else
            {
                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
                if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
                {
                    CheckStuck();
                }

                if (Time.time - BotOwner.Mover._lastTimePosChanged > 30f && BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
                {
                    BotOwner.StopMove();
                    BotOwner.Mover.AllowTeleport();
                    BotOwner.GetPlayer.Teleport(McsBotPlayerData.LeadPlayer.Position, true);
                    var playerPosition = McsBotPlayerData.Player.Position;
                    BotOwner.Mover._lastGoodCastPoint = BotOwner.Mover._prevSuccessLinkedFrom = BotOwner.Mover._prevLinkPos = BotOwner.Mover.PositionOnWayInner = playerPosition;
                    BotOwner.Mover._lastGoodCastPointTime = Time.time;
                    BotOwner.Mover._prevPosLinkedTime = 0f;
                    BotOwner.Mover.SetPlayerToNavMesh(playerPosition);
                    BotOwner.Mover.RecalcWay();
                    BotOwner.Mover.Pause = true;
                    UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                    }
                    return true;
                }

                if (Time.time - BotOwner.Mover._lastTimePosChanged >= 2f)
                {
                    return true;
                }

                return false;
            }
        }

        public virtual bool EndGoToProtect()
        {
            // 威胁中断：保护点前往途中威胁逼近时结束动作重评应战
            if (IsApproachingThreat())
            {
                return true;
            }

            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (BotOwner.GoToSomePointData.IsCome())
            {
                return true;
            }
            else
            {
                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
                if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
                {
                    CheckStuck();
                }

                if (Time.time - BotOwner.Mover._lastTimePosChanged > 30f && BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
                {
                    BotOwner.StopMove();
                    BotOwner.Mover.AllowTeleport();
                    BotOwner.GetPlayer.Teleport(McsBotPlayerData.LeadPlayer.Position, true);
                    var playerPosition = McsBotPlayerData.Player.Position;
                    BotOwner.Mover._lastGoodCastPoint = BotOwner.Mover._prevSuccessLinkedFrom = BotOwner.Mover._prevLinkPos = BotOwner.Mover.PositionOnWayInner = playerPosition;
                    BotOwner.Mover._lastGoodCastPointTime = Time.time;
                    BotOwner.Mover._prevPosLinkedTime = 0f;
                    BotOwner.Mover.SetPlayerToNavMesh(playerPosition);
                    BotOwner.Mover.RecalcWay();
                    BotOwner.Mover.Pause = true;
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Regroup
                    });
                    UpdateLeadNearMoveTarget(mcsLeadPlayerPos, out float nextTime);
                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                    }
                    return true;
                }

                if (Time.time - BotOwner.Mover._lastTimePosChanged > 6f)
                {
                    return true;
                }
                return false;
            }
        }

        public virtual bool EndAttackMoving()
        {
            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }
            var haveBullets = BotOwner.WeaponManager?.HaveBullets;
            if (!haveBullets.Value)
            {
                return true;
            }

            if (Time.time - BotOwner.ShootData.LastTriggerPressd > 9f)
            {
                return true;
            }
            if (BotOwner.DogFight.DogFightState > BotDogFightStatus.none)
            {
                return true;
            }
            if (BotOwner.Memory.IsInCover)
            {
                return true;
            }
            if (BotOwner.GoToSomePointData.IsCome())
            {
                return true;
            }
            return false;
        }

        protected bool CheckStuck()
        {
            var pos = BotOwner.Position;
            if (BotOwner.Mover._lastPos.McsSqrDistance(pos) > 2f * 2f)
            {
                BotOwner.Mover._lastPos = pos;
                BotOwner.Mover._lastTimePosChanged = Time.time;
                return false;
            }
            else
            {
                TrySolveStuck();
            }
            return true;
        }

        public virtual void TrySolveStuck()
        {
            // 卡门兜底：原地卡住且最近门（≤2m）为关闭态时直接快速开门（覆盖路径求交漏检：
            // 目标点恰在门前不远处时穿门线判定可能失败；门不可翻越，翻越解法对门无效）
            if (TryFastOpenNearestDoor())
            {
                return;
            }

            if (_nextVaultCheckTime < Time.time)
            {
                _nextVaultCheckTime = Time.time + VAULT_CHECK_INTERVAL;
                if (ShouldTryVault())
                {
                    if (!TryVault())
                    {

                    }
                }
            }
        }

        /// <summary>
        /// 卡门兜底开门：最近体素门（GetNearestDoor ≤2m）为关闭态且无冷却时快速开门
        /// </summary>
        private bool TryFastOpenNearestDoor()
        {
            var mcsBotPlayerData = McsBotPlayerData;
            if (mcsBotPlayerData == null || BotOwner.Memory.HaveEnemy)
            {
                return false;
            }

            var nearestDoorLink = BotOwner.NearDoorData.GetNearestDoor();
            var door = nearestDoorLink?.Door;
            if (door == null || door.DoorState != EDoorState.Shut)
            {
                return false;
            }

            var time = Time.time;
            if (mcsBotPlayerData.FastOpenDoorCooldowns.TryGetValue(door.Id, out var cooldownTime) && time < cooldownTime)
            {
                return false;
            }

            FastOpenDoor(door, time);
            return true;
        }

        public virtual bool EndHoldPosition()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            UpdateCoverToShoot();
            var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
            if (BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) > TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
            {
                return true;
            }

            if (_haveCoverToShoot && ProtectWantKill() && ProtectCareKill())
            {
                return true;
            }

            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (!BotOwner.Memory.IsInCover)
            {
                return true;
            }
            if (goalEnemy == null)
            {
                if (CanSearchEnemy())
                {
                    return true;
                }
            }
            else
            {
                if (goalEnemy.IsVisible && goalEnemy.CanShoot)
                {
                    return true;
                }
                if (goalEnemy.IsVisible && goalEnemy.Distance < 100f)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool CanSearchEnemy()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            return goalEnemy == null || !WasHitRecently(10f) && !goalEnemy.IsVisible && !goalEnemy.CanShoot && goalEnemy.CanISearch && BotOwner.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Attack) && BotOwner.Memory.LastEnemyVisionOld(BotInternalSettingsController.Core.COVER_SECONDS_AFTER_LOSE_VISION);
        }

        public virtual bool ProtectCareKill()
        {
            // return (Time.time - GetEnemyLastSeenTime()) < 10f;
            return true;
        }

        public virtual bool ProtectWantKill()
        {
            // return (Time.time - BotOwner.BotsGroup.EnemyLastSeenTimeReal) < BotOwner.Settings.FileSettings.Mind.ATTACK_ENEMY_IF_PROTECT_DELTA_LAST_TIME_SEEN;
            return true;
        }

        public virtual float GetEnemyLastSeenTime()
        {
            if (BotOwner.Settings.FileSettings.Mind.PROTECT_TIME_REAL)
            {
                return BotOwner.BotsGroup.EnemyLastSeenTimeReal;
            }
            return BotOwner.BotsGroup.EnemyLastSeenTimeSence;
        }

        public virtual CustomNavigationPoint FollowerCheckData()
        {
            Vector3 leadPos;
            if (McsBotPlayerData?.LeadPlayer != null && McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
            {
                leadPos = McsBotPlayerData.LeadPlayer.Position;
            }
            else if (BotOwner.BotFollower.HaveBoss)
            {
                leadPos = BotOwner.BotFollower.BossToFollow.Position;
            }
            else
            {
                leadPos = BotOwner.Position;
            }
            var shootPointClass = BotOwner.CurrentEnemyTargetPosition(true);
            var coverShootType = CoverShootType.shoot;
            if (shootPointClass == null)
            {
                coverShootType = CoverShootType.hide;
            }
            var coverSearchData = new CoverSearchData(leadPos, BotOwner.CoverSearchInfo, coverShootType, BotInternalSettingsController.Core.START_DIST_TO_COV, 0f, CoverSearchType.closerToSelectedPoint, shootPointClass, null, new Vector3?(leadPos), ECheckSHootHide.shootAndHide, new CoverSearchDefenceData(0f), PointsArrayType.byShootType, true);
            return BotOwner.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);
        }

        public virtual bool ShouldEndPatrol()
        {
            if (BotOwner.PeaceLook.HaveActions())
            {
                return true;
            }

            return false;
        }

        public virtual bool IsDogFighting()
        {
            return BotOwner.DogFight.DogFightState > BotDogFightStatus.none;
        }

        public virtual bool EndGoToLootTarget()
        {
            // 威胁中断：拾取目标前往途中威胁逼近时结束动作重评应战（LootingTarget/锁保留，战后续走）
            if (IsApproachingThreat())
            {
                return true;
            }

            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (Time.time > _nextLootingCheckTime && !McsBotPlayerData.IsTaskRunning && !McsBotPlayerData.IsLooting)
            {
                _currentLootingRetries += 1;
                _nextLootingCheckTime = Time.time + LOOTING_FINNISHED_COODDOWN;
                return true;
            }

            if (_currentLootingRetries >= 15)
            {
                _currentLootingRetries = 0;
                McsBotPlayerData.IsLooting = false;
                return true;
            }

            if (BotOwner.GoToSomePointData.IsCome())
            {
                return true;
            }
            return false;
        }

        public virtual bool ShouldShootImmediately()
        {
            try
            {
                var goalEnemy = BotOwner.Memory.GoalEnemy;
                var flag = ((goalEnemy != null && goalEnemy.Distance < BotOwner.Settings.FileSettings.Shoot.SHOOT_IMMEDIATELY_DIST) || BotOwner.BotsGroup.AnyBodyShootImmediately) && goalEnemy.CanShoot && Time.time - goalEnemy.AddTime < 5f;
                var isActive = BotOwner.WeaponManager.UnderbarrelLauncherController.IsActive;
                BotOwner.BotsGroup.AnyBodyShootImmediately = flag || isActive;
                return BotOwner.BotsGroup.AnyBodyShootImmediately;
            }
            catch
            {
                return false;
            }
        }

        public virtual bool IsShootFromCoverConditionAllFine()
        {
            if (!BotOwner.Memory.IsInCover)
            {
                return false;
            }
            if (!BotOwner.LookSensor.EnoughDistToShoot(out var flag))
            {
                return false;
            }
            if (!BotOwner.Memory.CurCustomCoverPoint.CanShootToTargetCast(BotOwner, BotOwner.Settings.FileSettings.Cover.DELTA_SEEN_FROM_COVE_LAST_POS))
            {
                return false;
            }
            if (BotOwner.WeaponManager.Stationary.ShallEndShootFromCurrent())
            {
                return false;
            }
            return true;
        }

        public virtual bool GetCrossPoint(EnemyInfo enemy)
        {
            var nearestDoor = BotOwner.NearDoorData.GetNearestDoor();
            if (nearestDoor == null)
            {
                return false;
            }
            var position = BotOwner.Transform.position;
            var currPosition = enemy.CurrPosition;
            var gclass = new SegmentPoints(position, currPosition);
            var vector = nearestDoor.SegmentOpen.b - nearestDoor.SegmentOpen.a;
            var vector2 = nearestDoor.SegmentOpen.a - vector * 0.1f;
            var vector3 = nearestDoor.SegmentOpen.b + vector * 0.1f;
            return AIUtility.GetCrossPoint(gclass.a, gclass.b, vector2, vector3) != null;
        }

        public virtual bool CannotSeeEnemy(EnemyInfo info)
        {
            if (info == null)
            {
                return false;
            }
            var vector = info.EnemyLastPositionReal + Vector3.up * 1.6f;
            return !Physics.Linecast(BotOwner.WeaponRoot.position, vector, out var raycastHit, LayersMaskController.HighPolyWithTerrainMask);
        }

        public virtual bool CanShootNow()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            return goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible;
        }

        public virtual bool ShootNow()
        {
            return BotOwner.Memory.GoalEnemy.CanShoot && BotOwner.Memory.GoalEnemy.IsVisible;
        }

        public virtual bool EndShootFromPlace()
        {
            if (!BotOwner.Memory.HaveEnemy)
            {
                return true;
            }
            if (BotOwner.DogFight.ShallStartCauseHavePlace())
            {
                return true;
            }
            if (!ShootNow())
            {
                return true;
            }
            if (WasHitRecently(5f))
            {
                return true;
            }
            if (_nextShootTime < Time.time)
            {
                _nextShootTime = Time.time + 3f;
                if (BotOwner.BotLay.CanShootPos(BotOwner.Memory.GoalEnemy, true, false))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool EndShootFromCover()
        {
            if (!BotOwner.Memory.IsInCover)
            {
                return true;
            }

            if (!BotOwner.LookSensor.EnoughDistToShoot(out var enough))
            {
                return true;
            }
            if (!BotOwner.Memory.CurCustomCoverPoint.CanShootToTargetCast(BotOwner, BotOwner.Settings.FileSettings.Cover.DELTA_SEEN_FROM_COVE_LAST_POS))
            {
                return true;
            }
            if (BotOwner.WeaponManager.Stationary.ShallEndShootFromCurrent())
            {
                return true;
            }
            return false;
        }

        public virtual bool EndGoToEnemy()
        {
            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }
            if (BotOwner.DogFight.ShallStartCauseHavePlace())
            {
                return true;
            }
            if (IsEnemyPosLost())
            {
                return true;
            }
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (!(BotOwner.DogFight.DogFightState > BotDogFightStatus.none) && goalEnemy != null && (!goalEnemy.IsVisible || !goalEnemy.CanShoot))
            {
                return false;
            }
            return true;
        }

        public virtual bool IsEnemyPosLost()
        {
            if (Time.time - BotOwner.Memory.LastEnemyTimeSeen > 10f)
            {
                BotOwner.Memory.GoalEnemy = null;
                return true;
            }
            return false;
        }

        public virtual bool EndShootToSmoke()
        {
            if (!BotOwner.SmokeGrenade.ShallShoot())
            {
                return true;
            }
            return false;
        }

        public virtual bool EndRunToEnemy()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (McsBotPlayerData.HasIntent(Intents.ShouldFollowMe))
            {
                return true;
            }

            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (BotOwner.DogFight.ShallStartCauseHavePlace())
            {
                return true;
            }
            if (IsEnemyPosLost())
            {
                return true;
            }

            if (BotOwner.Mover.IsComeTo(BotOwner.Settings.FileSettings.Move.REACH_DIST, false, null))
            {
                return true;
            }

            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null && (!goalEnemy.IsVisible || !goalEnemy.CanShoot))
            {
                return false;
            }
            return true;
        }

        public virtual bool EndShootFromStationary()
        {
            if (IsEnemyPosLost())
            {
                if (BotOwner.Medecine.FirstAid.Have2Do)
                {
                    return true;
                }
                if (BotOwner.Medecine.SurgicalKit.HaveWork)
                {
                    return true;
                }
            }
            var curLink = BotOwner.WeaponManager.Stationary.CurLink;
            if (curLink == null)
            {
                return true;
            }
            if (!curLink.HaveAmmo())
            {
                return true;
            }
            if (!curLink.IsFree(BotOwner.Id))
            {
                return true;
            }
            if (BotOwner.Memory.HaveEnemy && !BotOwner.WeaponManager.Stationary.IsEnemyAtSector(BotOwner.WeaponManager.Stationary.CurLink))
            {
                return true;
            }
            if (!BotOwner.Memory.HaveEnemy)
            {
                ScanSector(curLink);
            }
            return false;
        }

        public virtual bool EndGoToExfiltrationPoint()
        {
            // 威胁中断：撤离点前往途中威胁逼近时结束动作重评应战
            if (IsApproachingThreat())
            {
                return true;
            }

            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (Time.time - BotOwner.Mover._lastTimePosChanged > 30f)
            {
                BotOwner.StopMove();
                BotOwner.Mover.AllowTeleport();
                BotOwner.GetPlayer.Teleport(BotOwner.PatrollingData.ExfiltrationData.CachedExfiltrationPoint.Position, true);
                var playerPosition = McsBotPlayerData.Player.Position;
                BotOwner.Mover._lastGoodCastPoint = BotOwner.Mover._prevSuccessLinkedFrom = BotOwner.Mover._prevLinkPos = BotOwner.Mover.PositionOnWayInner = playerPosition;
                BotOwner.Mover._lastGoodCastPointTime = Time.time;
                BotOwner.Mover._prevPosLinkedTime = 0f;
                BotOwner.Mover.SetPlayerToNavMesh(playerPosition);
                BotOwner.Mover.RecalcWay();
                BotOwner.Mover.Pause = true;
                UpdateCommonMoveTarget(BotOwner.PatrollingData.ExfiltrationData.CachedExfiltrationPoint.Position, out float nextTime);
                if (_currentMoveTarget.HasValue)
                {
                    BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                }
                return true;
            }
            return false;
        }

        public virtual bool IsWannaLeave()
        {
            if (BotOwner.Boss.IamBoss || BotOwner.BotFollower == null || BotOwner.BotFollower.BossToFollow == null)
            {
                return BotOwner.Exfiltration.WannaLeave();
            }
            IPlayer player = BotOwner.BotFollower.BossToFollow.Player();
            if (player != null && player.AIData != null && !(player.AIData.BotOwner == null) && player.AIData.BotOwner.Exfiltration != null)
            {
                return player.AIData.BotOwner.Exfiltration.WannaLeave();
            }
            return BotOwner.Exfiltration.WannaLeave();
        }

        public virtual bool EndMeleeAttack()
        {
            var weaponManager = BotOwner.WeaponManager;
            if (weaponManager == null)
            {
                return true;
            }

            var meleeData = weaponManager.Melee;
            if (meleeData == null)
            {
                return true;
            }

            var goalEnemy = BotOwner.Memory.GoalEnemy;

            if (goalEnemy == null)
            {
                return true;
            }

            if (meleeData.ShallEndRun)
            {
                return true;
            }

            if (weaponManager.HaveBullets)
            {
                return true;
            }

            if ((Time.time - goalEnemy.PersonalLastSeenTime) > 5f)
            {
                return true;
            }

            if (IsEnemyPosLost())
            {
                return true;
            }

            if (weaponManager.Reload?.Reloading == true)
            {
                return true;
            }

            if (!weaponManager.IsMelee)
            {
                return true;
            }

            return false;
        }

        public virtual EquipmentSlot CheckWeaponSwitch(bool forceRefresh = false)
        {
            var weaponManager = BotOwner.WeaponManager;
            if (weaponManager == null || weaponManager.Selector == null)
            {
                return EquipmentSlot.FirstPrimaryWeapon;
            }

            var currentSlot = weaponManager.Selector.EquipmentSlot;
            if (_nextWeaponSwitchTime > Time.time)
            {
                return currentSlot;
            }

            weaponManager.Selector.UpdateWeaponsList();
            var targetSlot = BotOwner.DetermineWeaponSlotByAmmo(currentSlot, out var total);
            if (targetSlot != currentSlot)
            {
                BotOwner.TryChangeWeaponSlot(targetSlot);
                _nextWeaponSwitchTime = Time.time + WEAPON_SWITCH_COOLDOWN;
            }
            else if (forceRefresh)
            {
                BotOwner.TryChangeWeaponSlot(currentSlot);
                _nextWeaponSwitchTime = Time.time + WEAPON_SWITCH_COOLDOWN;
            }

            return targetSlot;
        }

        public virtual bool ShouldUseMeleeAttack()
        {
            if (_nextMeleeCheckTime > Time.time)
            {
                return false;
            }

            var weaponManager = BotOwner.WeaponManager;
            if (weaponManager == null)
            {
                return false;
            }

            var targetSlot = CheckWeaponSwitch();
#if DEBUG
            // McsLogger.LogWarning($"目标武器类型: {targetSlot}");
#endif
            _nextMeleeCheckTime = Time.time + MELEE_CHECK_INTERVAL;

            if (targetSlot == EquipmentSlot.Scabbard && weaponManager.IsMelee)
            {
                return true;
            }

            if (targetSlot == EquipmentSlot.Scabbard && !weaponManager.Selector.CanChangeToMeleeWeapons)
            {
                return false;
            }

            if (targetSlot == EquipmentSlot.Scabbard && !weaponManager.HaveBullets)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 参考SAIN
        /// </summary>
        /// <returns></returns>
        public virtual bool ShouldTryVault()
        {
            if (BotOwner.GetPlayer == null || BotOwner.GetPlayer.VaultingComponent == null || BotOwner.GetPlayer.VaultingGameplayRestrictions == null)
            {
                return false;
            }

            if (!BotOwner.GetPlayer.VaultingGameplayRestrictions.CanVaulting())
            {
                return false;
            }

            if (!BotOwner.Mover.IsMoving)
            {
                return false;
            }

            var lookDirection = BotOwner.GetPlayer.LookDirection.normalized;
            var targetDirection = BotOwner.Mover.NormDirCurPoint;
            if (Vector3.Dot(lookDirection, targetDirection) < DIRECTION_ALIGNMENT_THRESHOLD)
            {
                return false;
            }

            if (Time.time - BotOwner.Mover._lastTimePosChanged < 3f)
            {
                return false;
            }

            return true;
        }

        public virtual bool TryVault()
        {
            if (CheckForVaultableObstacle())
            {
                if (BotOwner.GetPlayer.VaultingComponent.TryVaulting())
                {
                    BotOwner.GetPlayer.OnVaulting();
                    return true;
                }
            }
            return false;
        }

        public virtual bool CheckForVaultableObstacle()
        {
            var startPosition = BotOwner.GetPlayer.WeaponRoot.position;
            var lookDirection = BotOwner.GetPlayer.LookDirection.normalized;
            var endPosition = startPosition + lookDirection * SPHERECAST_DISTANCE;

            startPosition.y += 0.33f;
            endPosition.y += 0.33f;

            if (Physics.SphereCast(startPosition, SPHERECAST_RADIUS, lookDirection, out RaycastHit hit, SPHERECAST_DISTANCE, LayersMaskController.PlayerStaticCollisionsMask))
            {
                if (hit.collider != null)
                {
                    var obstacleHeight = hit.collider.bounds.size.y;
                    var maxVaultHeight = BotOwner.GetPlayer.VaultingParameters.VaultingHeight;

                    return obstacleHeight < maxVaultHeight && obstacleHeight < VAULT_HEIGHT_THRESHOLD;
                }
            }

            return false;
        }

        public virtual void UpdateLeadNearMoveTarget(Vector3? leadPos, out float nextUpdateTime)
        {
            if (!leadPos.HasValue)
            {
                nextUpdateTime = 1f;
                return;
            }

            if (TryUpdateFormationMoveTarget(leadPos.Value, out nextUpdateTime))
            {
                return;
            }

            var sqrDistanceToLead = BotOwner.Position.McsSqrDistance(leadPos.Value);
            if (sqrDistanceToLead <= TOO_CLOSE_FROM_LEAD_DISTANCE * TOO_CLOSE_FROM_LEAD_DISTANCE)
            {
                var awayDir = BotOwner.Position - leadPos.Value;
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude < 0.0001f)
                {
                    awayDir = BotOwner.Transform.forward;
                    awayDir.y = 0f;
                }
                awayDir.Normalize();

                var retreatTarget = leadPos.Value + awayDir * (TOO_CLOSE_FROM_LEAD_DISTANCE + 1f);
                if (Tools.BetterDestination(3f, retreatTarget, out var betterDestination))
                {
                    retreatTarget = betterDestination;
                }

                _lastTargetPos = leadPos;
                _currentMoveTarget = retreatTarget;
                nextUpdateTime = 0.2f;
                return;
            }

            if (_lastTargetPos.Value.McsSqrDistance(leadPos.Value) < LEAD_POSITION_CHANGE_THRESHOLD * LEAD_POSITION_CHANGE_THRESHOLD)
            {
                nextUpdateTime = 1f;
                return;
            }

            var nearPos = Tools.GetPosNearTarget(leadPos.Value, BotOwner);
            if (!nearPos.HasValue)
            {
                nextUpdateTime = 0.25f;
                return;
            }

            var groundPos = TryProjectToGround(nearPos.Value);

            if (!CanGetPathToRun(BotOwner.Position, groundPos, McsBotPlayerData, out Vector3[] corners))
            {
                nextUpdateTime = 0.25f;
                return;
            }

            var newMoveTarget = GetPointAlongPathAtDistance(corners, 15f);
            _currentMoveTarget = newMoveTarget;
            nextUpdateTime = 1f;
        }

        public virtual bool TryUpdateFormationMoveTarget(Vector3 leadPos, out float nextUpdateTime)
        {
            nextUpdateTime = 1f;

            var mcsBotPlayerConfig = McsBotPlayerData?.McsAILeadPlayer?.McsBotPlayerConfig;
            if (mcsBotPlayerConfig == null)
            {
                return false;
            }

            if (!mcsBotPlayerConfig.EnableKeepFormation)
            {
                return false;
            }

            var leadPlayer = McsBotPlayerData?.LeadPlayer;
            if (leadPlayer == null)
            {
                return false;
            }

            var botIndex = Tools.GetMcsBotPlayerIndex(BotOwner.ProfileId, mcsBotPlayerConfig.FormationSequentialFill);
            if (botIndex < 5)
            {
                return false;
            }

            var predictedPos = leadPos + leadPlayer.Velocity * 2;
            if (NavMesh.SamplePosition(predictedPos, out var hit, 3f, -1))
            {
                predictedPos = hit.position;
            }
            else
            {
                predictedPos = leadPos;
            }

            var target = Tools.ComputeTarget(leadPlayer, predictedPos, botIndex, Tools.ParseFormationMatrix(mcsBotPlayerConfig.FormationMatrix), mcsBotPlayerConfig.FormationSpacing);
            if (!target.HasValue)
            {
                return false;
            }

            var groundPos = TryProjectToGround(target.Value);

            if (!CanGetPathToRun(BotOwner.Position, groundPos, McsBotPlayerData, out Vector3[] corners))
            {
                nextUpdateTime = 0.25f;
                return true;
            }

            var newMoveTarget = GetPointAlongPathAtDistance(corners, 15f);
            _currentMoveTarget = newMoveTarget;
            nextUpdateTime = 0.2f;
            return true;
        }

        public virtual void UpdateEscortMoveTarget(Vector3? escortPos, out float nextUpdateTime)
        {
            if (McsBotPlayerData == null)
            {
                nextUpdateTime = 1f;
                return;
            }

            if (!escortPos.HasValue)
            {
                nextUpdateTime = 0.25f;
                return;
            }

            if (_lastTargetPos != escortPos)
            {
                _lastTargetPos = escortPos;
                _lastCalcCorners = null;
                _lastCanRunResult = false;
                _currentMoveRetries = 0;
                _currentMoveTarget = escortPos;
            }

            var leadPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
            if (leadPos == null)
            {
                nextUpdateTime = 1f;
                return;
            }

            var leadVelocity = McsBotPlayerData.LeadPlayer.Velocity;
            var predictedPos = leadPos + leadVelocity * 2;

            if (NavMesh.SamplePosition(predictedPos, out var hit, 1f, -1))
            {
                predictedPos = hit.position;
            }
            else
            {
                predictedPos = leadPos;
            }

            var groundPos = TryProjectToGround(predictedPos);

            if (!CanGetPathToRun(groundPos, escortPos.Value, McsBotPlayerData, out Vector3[] corners))
            {
                nextUpdateTime = 0.25f;
                return;
            }

            var newMoveTarget = GetPointAlongPathAtDistance(corners, 15f);
            _currentMoveTarget = newMoveTarget;
            nextUpdateTime = 0.2f;
        }

        public virtual void UpdateCommonMoveTarget(Vector3? targetPos, out float nextUpdateTime)
        {
            if (McsBotPlayerData == null)
            {
                nextUpdateTime = 1f;
                return;
            }

            if (!targetPos.HasValue)
            {
                nextUpdateTime = 0.25f;
                return;
            }

            if (_lastTargetPos != targetPos)
            {
                _lastTargetPos = targetPos;
                _lastCalcCorners = null;
                _lastCanRunResult = false;
                _currentMoveRetries = 0;
                _currentMoveTarget = targetPos;
            }

            var selfVelocity = BotOwner.Velocity;
            var predictedPos = BotOwner.Position + selfVelocity * 2;

            if (NavMesh.SamplePosition(predictedPos, out var hit, 1f, -1))
            {
                predictedPos = hit.position;
            }
            else
            {
                predictedPos = BotOwner.Position;
            }

            var startGroundPos = TryProjectToGround(predictedPos);
            var targetGroundPos = TryProjectToGround(targetPos.Value);

            if (!CanGetPathToRun(startGroundPos, targetGroundPos, McsBotPlayerData, out Vector3[] corners))
            {
                nextUpdateTime = 0.25f;
                return;
            }

            var newMoveTarget = GetPointAlongPathAtDistance(corners, 30f);
            _currentMoveTarget = newMoveTarget;
            nextUpdateTime = 1f;
        }

        public virtual bool CanGetPathToRun(Vector3 startPos, Vector3 targetPos, McsBotPlayerData mcsBotPlayerData, out Vector3[] corners)
        {
            var navMeshPath = new NavMeshPath();
            NavMesh.CalculatePath(startPos, targetPos, -1, navMeshPath);
            var flag = false;

            var sqrDistanceToTarget = startPos.McsSqrDistance(targetPos);
            var sampleRadius = sqrDistanceToTarget > 50f * 50f ? 5f : 1.5f;

            if (navMeshPath.status is NavMeshPathStatus.PathComplete or NavMeshPathStatus.PathPartial)
            {
                flag = true;
                if ((targetPos - navMeshPath.corners[navMeshPath.corners.Length - 1]).magnitude > Math.Max(2f, sampleRadius))
                {
                    flag = false;
                }
            }

            if (!flag && Tools.BetterDestination(sampleRadius, targetPos, out var betterDest))
            {
                navMeshPath = new NavMeshPath();
                NavMesh.CalculatePath(startPos, betterDest, -1, navMeshPath);
                if (navMeshPath.status is NavMeshPathStatus.PathComplete or NavMeshPathStatus.PathPartial)
                {
                    flag = true;
                }
            }

            if (!flag)
            {
                _currentMoveRetries += 1;
                if (_currentMoveRetries >= 5 || !_lastCanRunResult)
                {
                    _currentMoveRetries = 0;
                    corners = null;
                    _lastCanRunResult = false;
                    mcsBotPlayerData.SetIntent([Intents.ShouldFollowMe, Intents.ShouldKeepFormation]);
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
                    return _lastCanRunResult;
                }

                corners = _lastCalcCorners;
                return _lastCanRunResult;
            }

            _currentMoveRetries = 0;
            _lastCalcCorners = navMeshPath.corners;
            corners = _lastCalcCorners;
            _lastCanRunResult = true;

            // 快速开门：新路径产出时检测前段是否穿过关闭门（跟随/护送/通用移动的唯一角点咽喉）
            TryFastOpenDoorOnPath(navMeshPath.corners);
            return _lastCanRunResult;
        }

        /// <summary>
        /// 快速开门（参考 SAIN DoorOpener 思路，以 MCS 风格落地）：移动路径与关闭门求交命中时——
        /// 指令直开（Player.ExecuteInteraction，门当帧开始摆动，绕过原生 LayHand 动画对位链）+ 碰撞忽略窗口
        /// （不等门完全打开即通过）。原生链不删不改：门变 Interacting/Open 后原生 WantPassTheDoor 不再触发，
        /// 2.5s 冻结/半速接近/进门仪式天然绕过。仅非战斗移动使用（战斗近区由 SAIN 门逻辑接管，远区保留原生链）
        /// </summary>
        protected void TryFastOpenDoorOnPath(Vector3[] corners)
        {
            var mcsBotPlayerData = McsBotPlayerData;

            // 窗口收尾（PlayerDataMgr 1s 保险循环同样调用，防移动刷新停止后碰撞被永久忽略）
            mcsBotPlayerData?.TryFinishFastOpenDoor();
            if (mcsBotPlayerData == null)
            {
                return;
            }

            // 仅非战斗移动：战斗期间不快速开门
            if (BotOwner.Memory.HaveEnemy)
            {
                return;
            }

            if (corners == null || corners.Length < 2)
            {
                return;
            }

            var time = Time.time;
            var doorLinks = BotOwner.NearDoorData.CurrentDoorLinks();
            if (doorLinks.Count == 0)
            {
                return;
            }

            var botPosition = BotOwner.Position;
            var sqrCheckDist = FAST_OPEN_DOOR_PATH_CHECK_DIST * FAST_OPEN_DOOR_PATH_CHECK_DIST;
            for (var linkIndex = 0; linkIndex < doorLinks.Count; linkIndex++)
            {
                var doorLink = doorLinks[linkIndex];
                var door = doorLink?.Door;
                if (door == null || door.DoorState != EDoorState.Shut)
                {
                    continue;
                }

                if (botPosition.McsSqrDistance(doorLink.MidOpen) > sqrCheckDist)
                {
                    continue;
                }

                if (mcsBotPlayerData.FastOpenDoorCooldowns.TryGetValue(door.Id, out var cooldownTime) && time < cooldownTime)
                {
                    continue;
                }

                if (!IsPathCrossingDoorOpen(corners, doorLink))
                {
                    continue;
                }

                FastOpenDoor(door, time);
                return;
            }
        }

        /// <summary>
        /// 路径前段（累计 FAST_OPEN_DOOR_PATH_LOOKAHEAD 米内）是否穿越门洞线
        /// （SegmentOpen 两端各外延 0.1，几何判定同 GetCrossPoint 原生用法；楼层防护过滤跨层门）
        /// </summary>
        private bool IsPathCrossingDoorOpen(Vector3[] corners, NavMeshDoorLink doorLink)
        {
            var segmentOpen = doorLink.SegmentOpen;
            var openDir = segmentOpen.b - segmentOpen.a;
            var openA = segmentOpen.a - openDir * 0.1f;
            var openB = segmentOpen.b + openDir * 0.1f;

            var accumulated = 0f;
            for (var i = 0; i < corners.Length - 1; i++)
            {
                var segmentLength = Vector3.Distance(corners[i], corners[i + 1]);

                if (Mathf.Abs(corners[i].y - doorLink.MidOpen.y) > FAST_OPEN_DOOR_FLOOR_HEIGHT_DIFF)
                {
                    // 跨层段：跳过但计入前段长度
                    accumulated += segmentLength;
                    if (accumulated >= FAST_OPEN_DOOR_PATH_LOOKAHEAD)
                    {
                        return false;
                    }
                    continue;
                }

                if (AIUtility.GetCrossPoint(corners[i], corners[i + 1], openA, openB) != null)
                {
                    return true;
                }

                accumulated += segmentLength;
                if (accumulated >= FAST_OPEN_DOOR_PATH_LOOKAHEAD)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 执行快速开门：可交互校验 → 指令直开 → 碰撞忽略窗口 + 同门冷却记录
        /// </summary>
        private void FastOpenDoor(Door door, float time)
        {
            var player = BotOwner.GetPlayer;
            if (player == null || player.MovementContext.CanInteract != null)
            {
                return;
            }

            player.ExecuteInteraction(door, new InteractionResult(EInteractionType.Open));

            if (door.Collider != null)
            {
                player.MovementContext.IgnoreInteractionCollision(door.Collider, true);
            }

            var mcsBotPlayerData = McsBotPlayerData;
            mcsBotPlayerData.FastOpenDoor = door;
            mcsBotPlayerData.FastOpenDoorEndTime = time + FAST_OPEN_DOOR_WINDOW;
            mcsBotPlayerData.FastOpenDoorCooldowns[door.Id] = time + FAST_OPEN_DOOR_COOLDOWN;
        }

        public virtual Vector3 GetPointAlongPathAtDistance(Vector3[] corners, float distance)
        {
            var accumulated = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                var segLen = Vector3.Distance(corners[i], corners[i + 1]);
                if (accumulated + segLen >= distance)
                {
                    var t = (distance - accumulated) / segLen;
                    return Vector3.Lerp(corners[i], corners[i + 1], t);
                }
                accumulated += segLen;
            }
            return corners[corners.Length - 1];
        }

        public virtual Vector3 TryProjectToGround(Vector3 pos)
        {
            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var rayHit, 50f, LayersMaskController.HighPolyWithTerrainMask))
            {
                if (NavMesh.SamplePosition(rayHit.point, out var navHit1, 1f, -1))
                {
                    return navHit1.position;
                }
                return rayHit.point;
            }

            if (NavMesh.SamplePosition(pos, out var navHit2, 10f, -1))
            {
                return navHit2.position;
            }

            return pos;
        }

        public virtual bool EndDeactivateMine()
        {
            // 威胁中断：拆雷途中威胁逼近时结束动作重评应战（雷未拆完战后可续）
            if (IsApproachingThreat())
            {
                return true;
            }

            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (!BotOwner.BewarePlantedMine.CanDeactivate())
            {
                return true;
            }

            if (Time.time > _nextDeactivateCheckTime)
            {
                _currentDeactivateRetries += 1;
                _nextDeactivateCheckTime = Time.time + 1f;
            }

            if (_currentDeactivateRetries >= 15)
            {
                _currentDeactivateRetries = 0;
                return true;
            }
            return false;
        }

        public virtual bool EndRunAwayGrenade()
        {
            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (!BotOwner.BewareGrenade.ShallRunAway())
            {
                return true;
            }

            if (BotOwner.Memory.IsInCover)
            {
                return true;
            }

            return false;
        }

        public virtual bool EndRunAwayArtillery()
        {
            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (!BotOwner.ArtilleryDangerPlace.ShallRunAway())
            {
                return true;
            }

            if (BotOwner.Memory.IsInCover)
            {
                return true;
            }

            return false;
        }

        public virtual bool EndRunAwayBTR()
        {
            if (BotOwner.Mover._lastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (!BotOwner.BewareBTR.ShallRunAway())
            {
                return true;
            }

            if (BotOwner.Memory.IsInCover)
            {
                return true;
            }

            return false;
        }

        public virtual bool EndFlashed()
        {
            if (!BotOwner.FlashGrenade.IsFlashed)
            {
                return true;
            }

            return false;
        }

        public virtual bool EndGoToExcuteProxyAction()
        {
            // 威胁中断：代捡/任务/交互/固定武器前往途中威胁逼近时结束动作重评应战
            // （intent/TargetPos/ProxyTargetId 保留，战后自动续走）
            if (IsApproachingThreat())
            {
                return true;
            }

            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (!McsBotPlayerData.HasAnyIntent(Intents.ShouldInteractionProxyAction, Intents.ShouldLootProxyAction, Intents.ShouldQuestProxyAction))
            {
                return true;
            }
            return false;
        }

        public virtual bool EndDropTargetLootLogic()
        {
            // 威胁中断：丢弃战利品途中威胁逼近时结束动作重评应战（物品未丢完战后可续）
            if (IsApproachingThreat())
            {
                return true;
            }

            if (McsBotPlayerData == null)
            {
                return true;
            }

            var haveItemsToDrop = BotOwner.ExternalItemsController.HaveItemsToDrop();
            if (!haveItemsToDrop)
            {
                McsBotPlayerData.RemoveIntent([Intents.ShouldDropTargetLoot]);
                _nextLootingCheckTime = Time.time + ENTER_COMMON_LOOTING_COODDOWN * 2;
                return true;
            }

            var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
            var sqrDistance = BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos);
            if (sqrDistance > 9f)
            {
                return true;
            }

            return false;
        }

        public void RefreshStuckTimer()
        {
            BotOwner.Mover._lastTimePosChanged = Time.time;
        }

        public virtual bool CheckFirearmsAnimatorState()
        {
            BotOwner.WeaponManager.CheckWeaponReady();
            var time = Time.time;
            if (time < _nextAnimatorFixTime)
            {
                _nextAnimatorFixTime = time + 1;
                return true;
            }

            var player = BotOwner.GetPlayer;
            var firearmController = player?.HandsController as Player.FirearmController;
            if (firearmController == null)
            {
                _nextAnimatorFixTime = time + 1;
                BotOwner.WeaponManager.Selector.TryChangeToMain();
                return false;
            }

            if (firearmController?.FirearmsAnimator == null)
            {
                _nextAnimatorFixTime = time + 1;
                BotOwner.WeaponManager.Selector.TryChangeToMain();
                return false;
            }

            var handsIdle = !player.HandsController.IsAiming
                        && !player.HandsController.IsInventoryOpen()
                        && !player.HandsController.IsInInteractionStrictCheck()
                        && !player.HandsController.IsHandsProcessing();

            if (!BotOwner.WeaponManager.Selector.IsWeaponReady && !handsIdle)
            {
                _nextAnimatorFixTime = time + 1;
                BotOwner.WeaponManager.Selector.TryChangeToMain();
                return false;
            }

            _nextAnimatorFixTime = time + 1;
            return true;
        }

        public virtual bool TryGetBtrFollowAction(float time, out Action action)
        {
            action = null;

            var btrController = Singleton<GameWorld>.Instance.BtrController;
            if (btrController == null || btrController.BtrVehicle == null || btrController.BtrView == null)
            {
                return false;
            }

            var leadPlayer = McsBotPlayerData.LeadPlayer;
            if (leadPlayer == null)
            {
                return false;
            }

            var btrVehicle = btrController.BtrVehicle;
            var selfPlayer = BotOwner.GetPlayer;
            var selfIsPassenger = btrVehicle.IsPassenger(selfPlayer, out var selfPassenger);
            var bossInBtr = leadPlayer.BtrState == EPlayerBtrState.Inside || leadPlayer.BtrState == EPlayerBtrState.GoIn;
            var bossOutBtr = leadPlayer.BtrState == EPlayerBtrState.Outside || leadPlayer.BtrState == EPlayerBtrState.GoOut;

            if (bossInBtr && !selfIsPassenger)
            {
                if (!TryFindFreeSeat(btrController, out byte sideId, out byte slotId, out Vector3 doorPos))
                {
                    return false;
                }

                McsBotPlayerData.BtrTargetSide = sideId;
                McsBotPlayerData.BtrTargetSlot = slotId;
                McsBotPlayerData.IsBtrLeaving = false;

                if (_nextUpdatePosTime < time)
                {
                    UpdateCommonMoveTarget(doorPos, out float nextTime);
                    _nextUpdatePosTime = time + nextTime;
                }

                if (_currentMoveTarget.HasValue)
                {
                    BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                }

                action = new Action(typeof(GoToBtrLogic), "Mcs:GoToBtr");
                return true;
            }

            if (selfIsPassenger && !bossOutBtr)
            {
                McsBotPlayerData.IsBtrLeaving = false;
                action = new Action(typeof(HoldPositionLogic), "Mcs:BtrStay");
                return true;
            }

            if (selfIsPassenger && bossOutBtr)
            {
                McsBotPlayerData.IsBtrLeaving = true;
                McsBotPlayerData.BtrTargetSide = selfPassenger.SideId;
                McsBotPlayerData.BtrTargetSlot = selfPassenger.SlotId;
                action = new Action(typeof(GoToBtrLogic), "Mcs:LeaveBtr");
                return true;
            }
            return false;
        }

        public virtual bool TryFindFreeSeat(BtrController btrController, out byte sideId, out byte slotId, out Vector3 doorPos)
        {
            sideId = 0;
            slotId = 0;
            doorPos = Vector3.zero;

            for (byte s = 0; s <= 1; s++)
            {
                var side = btrController.BtrView.GetBtrSide(s);
                if (side == null)
                {
                    continue;
                }

                var info = side.SideInfo();
                for (byte slot = 0; slot < info.Length; slot++)
                {
                    if (info[slot])
                    {
                        sideId = s;
                        slotId = slot;
                        doorPos = side.GoInPoints().Item1;
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual bool IsTargetPitchReachable(StationaryWeapon stationaryWeapon, Vector3 targetPos)
        {
            var origin = stationaryWeapon.OperatorPosition;
            var delta = targetPos - origin;

            var horizontal = new Vector3(delta.x, 0f, delta.z).magnitude;
            if (horizontal < 0.01f)
            {
                return false;
            }

            var requiredPitch = Mathf.Atan2(-delta.y, horizontal) * Mathf.Rad2Deg;
            var pitchLimit = stationaryWeapon.PitchLimit;
            var min = Mathf.Min(pitchLimit.x, pitchLimit.y);
            var max = Mathf.Max(pitchLimit.x, pitchLimit.y);

            return requiredPitch >= min - 2f && requiredPitch <= max + 2f;
        }

        public virtual void ScanSector(StationaryWeaponLink link)
        {
            var weapon = link.Weapon;
            if (weapon == null)
            {
                return;
            }

            var halfAngleDeg = Mathf.Acos(Mathf.Clamp(link.CosAngleBase, -1f, 1f)) * Mathf.Rad2Deg;

            _scanPhase += Time.deltaTime / SCANPERIOD;
            var tri = Mathf.PingPong(_scanPhase, 1f);
            var yawDeg = Mathf.Lerp(-halfAngleDeg, halfAngleDeg, tri);

            var baseDir = link.InitialDir;
            baseDir.y = 0f;
            if (baseDir.sqrMagnitude < 0.001f)
            {
                return;
            }
            baseDir.Normalize();

            var dir = Quaternion.AngleAxis(yawDeg, Vector3.up) * baseDir;
            dir = Quaternion.AngleAxis(SCANPITCHDOWN, Vector3.Cross(dir, Vector3.up)) * dir;
            var scanPoint = weapon.OperatorPosition + dir * SCANDISTANCE;
            BotOwner.AimingManager.CurrentAiming.SetTarget(scanPoint);
            BotOwner.AimingManager.NodeUpdate();
        }
    }
}