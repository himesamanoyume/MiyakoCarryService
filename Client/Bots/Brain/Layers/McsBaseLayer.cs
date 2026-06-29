using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public abstract class McsBaseLayer<T> : CustomLayer where T : McsBaseLayer<T>
    {
        protected McsBaseLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            InitActionMap();
        }

        private bool? _isMcsBotPlayer = null;

        public bool IsMcsBotPlayer => _isMcsBotPlayer ??= BotOwner.IsMcsBotPlayer;
        protected Dictionary<Type, Func<bool>> _endActionMap;
        protected bool _haveCoverToShoot = false;
        protected float _nextHoldPositionTime = 0f;
        protected float _goToCoverTime = 0f;
        protected CustomNavigationPoint _currentNavigationPoint = null;
        protected float _nextPatrolTime = 0f;
        protected float _nextShootTime = 0f;
        protected float _nextWeaponSwitchTime = 0f;
        protected float _nextMeleeCheckTime = 0f;
        protected float _nextLootingCheckTime = 0f;
        protected float _nextVaultCheckTime = 0f;
        protected float _nextUpdatePosTime = 0f;
        protected float _nextHealCheckTime = 0f;
        protected Vector3? _currentMoveTarget = null;
        private Vector3? _lastTargetPos = Vector3.zero;
        private Vector3[] _lastCalcCorners = null;
        private bool _lastCanRunResult = false;
        private int _currentMoveRetries = 0;
        private int _currentHealTimes = 0;
        protected const float LEAD_POSITION_CHANGE_THRESHOLD = 2f;
        protected const float TOO_FAR_FROM_LEAD_DISTANCE = 20f;
        protected const float TOO_CLOSE_FROM_LEAD_DISTANCE = 2f;
        protected const float HEAL_CHECK_INTERVAL = 1f;
        protected const float VAULT_CHECK_INTERVAL = 2f;
        protected const float VAULT_HEIGHT_THRESHOLD = 1.5f;
        protected const float SPHERECAST_RADIUS = 0.1f;
        protected const float SPHERECAST_DISTANCE = 2f;
        protected const float DIRECTION_ALIGNMENT_THRESHOLD = 0.85f;
        protected const float ENTER_COMMON_LOOTING_COLDDOWN = 10f;
        protected const float LOOTING_FINNISHED_COLDDOWN = 5f;
        protected const float WEAPON_SWITCH_COOLDOWN = 1f;
        protected const float MELEE_CHECK_INTERVAL = 0.5f;

        public McsBotPlayerData McsBotPlayerData
        {
            get
            {
                return field ??= BotOwner.GetMcsBotPlayerData();
            }
        }

        private string Name
        {
            get
            {
                return field ??= typeof(T).Name;
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
                // 如果不执行这段代码，当护航从SAIN的Layer回到Mcs的Layer时，就会卡住不动
                var getSAINMethod = AccessTools.Method(Type.GetType("SAIN.SAINEnableClass, SAIN"), "GetSAIN");
                if (getSAINMethod == null)
                {
                    return;
                }

                var parameters = new object[] { BotOwner.ProfileId, null };
                getSAINMethod.Invoke(null, parameters);

                if (parameters[1] is not object sainBot || sainBot == null)
                {
                    return;
                }

                var sainBotTraverse = Traverse.Create(sainBot);
                var botActivation = sainBotTraverse.Property("BotActivation").GetValue();
                if (botActivation != null)
                {
                    var botActivationTraverse = Traverse.Create(botActivation);
                    botActivationTraverse.Property("ActiveLayer").SetValue(0); // ESAINLayer.None = 0  
                    botActivationTraverse.Method("ManualUpdate").GetValue();
                }
            }
        }

        protected SubtitlesMgr SubtitlesMgr => MgrAccessor.Get<SubtitlesMgr>();

        public override bool IsCurrentActionEnding()
        {
            if (CurrentAction == null)
            {
                return true;
            }

            return _endActionMap.TryGetValue(CurrentAction.Type, out var endFunc) ? endFunc() : true;
        }

        protected virtual void InitActionMap()
        {
            _endActionMap = new()
            {
                { typeof(GoToCoverPointLogic), EndGoToCoverPoint },
                { typeof(HealLogic), EndHeal },
                { typeof(RunToCoverLogic), EndRunToCover },
                { typeof(SimplePatrolLogic), EndSimplePatrol },
                { typeof(HoldPositionLogic), EndHoldPosition },
                { typeof(GoToPointLogic), EndGoToPoint },
                { typeof(GoToProtectLogic), EndGoToProtect },
                { typeof(GoToEnemyLogic), EndGoToEnemy },
                { typeof(AttackMovingLogic), EndAttackMoving },
                { typeof(GoToLootTargetLogic), EndGoToLootTarget },
                { typeof(ShootFromPlaceLogic), EndShootFromPlace },
                { typeof(ShootFromCoverLogic), EndShootFromCover },
                { typeof(ShootToSmokeLogic), EndShootToSmoke },
                { typeof(ShootFromStationaryLogic), EndShootFromStationary },
                { typeof(RunToEnemyLogic), EndRunToEnemy },
                { typeof(GoToExfiltrationPointNodeLogic), EndGoToExfiltrationPoint },
                { typeof(MeleeAttackLogic), EndMeleeAttack },
                { typeof(RunToPointLogic), EndGoToPoint },
                { typeof(EscortToPointByWayLogic), EndEscortToPointByWay },
                { typeof(FlashedLogic), EndFlashed },
                { typeof(DeactivateMineLogic), EndDeactivateMine },
                { typeof(RunAwayGrenadeLogic), EndRunAwayGrenade },
                { typeof(RunAwayArtilleryLogic), EndRunAwayArtillery },
                { typeof(RunAwayBTRLogic), EndRunAwayBTR },
                { typeof(GoToExcuteProxyActionLogic), EndGoToExcuteProxyAction },
            };
        }

        protected virtual bool EndHeal()
        {
            if (BotOwner.Medecine.Using)
            {
                return false;
            }

            if (BaseLogicLayerSimpleAbstractClass.CheckMedsToStop(BotOwner))
            {
                BotOwner.Medecine.FirstAid.CancelCurrent();
                if (BotOwner.Medecine.SurgicalKit.Using)
                {
                    BotOwner.Medecine.SurgicalKit.CancelCurrent();
                }
                _currentHealTimes = 0;
                return true;
            }

            var firstAidHasWork = BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use;
            var surgicalHasWork = BotOwner.Medecine.SurgicalKit.HaveWork;
            if (!firstAidHasWork && !surgicalHasWork)
            {
                _currentHealTimes = 0;
                return true;
            }

            if (Time.time > _nextHealCheckTime)
            {
                _nextHealCheckTime = Time.time + HEAL_CHECK_INTERVAL;
                _currentHealTimes += 1;
            }

            if (_currentHealTimes >= 15)
            {
                var player = BotOwner.GetPlayer;
                if (!BotOwner.Medecine.Using
                    && player.HandsController is Player.FirearmController firearmController
                    && !firearmController.IsAiming
                    && !firearmController.IsInReloadOperation()
                    && !firearmController.IsInventoryOpen()
                    && !firearmController.IsInInteractionStrictCheck()
                    && !firearmController.IsInSpawnOperation()
                    && !firearmController.IsHandsProcessing())
                {
                    CheckWeaponSwitch();
                }
                _currentHealTimes = 0;
                return true;
            }
            return false;
        }

        protected virtual bool EndRunToCover()
        {
            var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
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

        protected virtual void TryFindCover(Vector3 mcsLeadPlayerPos)
        {
            if (_goToCoverTime < Time.time)
            {
                _goToCoverTime = Time.time + 1f;
                var coverSearchData = new CoverSearchData(mcsLeadPlayerPos, BotOwner.CoverSearchInfo, CoverShootType.hide, TOO_FAR_FROM_LEAD_DISTANCE, 0f, CoverSearchType.closerToSelectedPoint, null, null, new Vector3?(mcsLeadPlayerPos), ECheckSHootHide.shootAndHide, new CoverSearchDefenceDataClass(0f), PointsArrayType.byShootType, true, null, null, "Default");
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

        protected virtual void UpdateCoverToShoot()
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

        protected virtual bool WasHitRecently(float timeframe)
        {
            return (Time.time - BotOwner.Memory.LastTimeHit) < timeframe;
        }

        protected virtual bool EndEatDrink()
        {
            return true;
        }

        protected virtual bool EndGoToCoverPoint()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
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

        protected virtual bool EndSimplePatrol()
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

        protected virtual bool EndGoToPoint()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (BotOwner.GoToSomePointData.IsCome())
            {
                if (McsBotPlayerData.HasDecision(EDecision.ShouldGoToPoint) && BotOwner.Position.McsSqrDistance(McsBotPlayerData.TargetPos.Value) <= 2f * 2f)
                {
                    McsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldHoldPosition);
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
                if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
                {
                    CheckStuck();
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged > 30f && BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
                {
                    BotOwner.StopMove();
                    BotOwner.Mover.AllowTeleport();
                    BotOwner.GetPlayer.Teleport(McsBotPlayerData.LeadPlayer.Position, true);
                    var playerPosition = McsBotPlayerData.Player.Position;
                    BotOwner.Mover.LastGoodCastPoint = BotOwner.Mover.PrevSuccessLinkedFrom_1 = BotOwner.Mover.PrevLinkPos = BotOwner.Mover.PositionOnWayInner = playerPosition;
                    BotOwner.Mover.LastGoodCastPointTime = Time.time;
                    BotOwner.Mover.PrevPosLinkedTime_1 = 0f;
                    BotOwner.Mover.SetPlayerToNavMesh(playerPosition);
                    BotOwner.Mover.RecalcWay();
                    BotOwner.Mover.Pause = true;
                    return true;
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged > 6f)
                {
                    if (McsBotPlayerData.HasDecision(EDecision.ShouldGoToPoint) && BotOwner.Position.McsSqrDistance(McsBotPlayerData.TargetPos.Value) <= 2f * 2f)
                    {
                        McsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldHoldPosition);
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

        private async Task DelaySetDecisions(float delaySeconds, EDecision[] exclude = null, params EDecision[] decisions)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.SetDecision(exclude, decisions);
            }
        }

        protected virtual bool EndEscortToPointByWay()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (!McsBotPlayerData.TargetPos.HasValue)
            {
                return true;
            }

            var sqrDistance = McsBotPlayerData.TargetPos.Value.McsSqrDistance(BotOwner.Position);
            if (sqrDistance < 2f * 2f)
            {
                if (McsBotPlayerData.HasDecision(EDecision.ShouldEscort))
                {
                    McsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldHoldPosition);
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.OnPosition
                    });
                    TasksExtensions.HandleExceptions(DelaySetDecisions(3f, [EDecision.ShouldRegroup, EDecision.ShouldGoToPoint, EDecision.ShouldEscort, EDecision.ShouldGoToPoint]));
                }
                return true;
            }
            else if (BotOwner.GoToSomePointData.IsCome())
            {
                return true;
            }
            else
            {
                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(McsBotPlayerData);
                if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
                {
                    CheckStuck();
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged > 30f && BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
                {
                    BotOwner.StopMove();
                    BotOwner.Mover.AllowTeleport();
                    BotOwner.GetPlayer.Teleport(McsBotPlayerData.LeadPlayer.Position, true);
                    var playerPosition = McsBotPlayerData.Player.Position;
                    BotOwner.Mover.LastGoodCastPoint = BotOwner.Mover.PrevSuccessLinkedFrom_1 = BotOwner.Mover.PrevLinkPos = BotOwner.Mover.PositionOnWayInner = playerPosition;
                    BotOwner.Mover.LastGoodCastPointTime = Time.time;
                    BotOwner.Mover.PrevPosLinkedTime_1 = 0f;
                    BotOwner.Mover.SetPlayerToNavMesh(playerPosition);
                    BotOwner.Mover.RecalcWay();
                    BotOwner.Mover.Pause = true;
                    return true;
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged >= 2f)
                {
                    return true;
                }

                return false;
            }
        }

        protected virtual bool EndGoToProtect()
        {
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
                if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
                {
                    CheckStuck();
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged > 30f && BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
                {
                    BotOwner.StopMove();
                    BotOwner.Mover.AllowTeleport();
                    BotOwner.GetPlayer.Teleport(McsBotPlayerData.LeadPlayer.Position, true);
                    var playerPosition = McsBotPlayerData.Player.Position;
                    BotOwner.Mover.LastGoodCastPoint = BotOwner.Mover.PrevSuccessLinkedFrom_1 = BotOwner.Mover.PrevLinkPos = BotOwner.Mover.PositionOnWayInner = playerPosition;
                    BotOwner.Mover.LastGoodCastPointTime = Time.time;
                    BotOwner.Mover.PrevPosLinkedTime_1 = 0f;
                    BotOwner.Mover.SetPlayerToNavMesh(playerPosition);
                    BotOwner.Mover.RecalcWay();
                    BotOwner.Mover.Pause = true;
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Regroup
                    });
                    return true;
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged > 6f)
                {
                    return true;
                }
                return false;
            }
        }

        protected virtual bool EndAttackMoving()
        {
            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
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
            if (BotOwner.Mover.LastPos.McsSqrDistance(pos) > 2f * 2f)
            {
                BotOwner.Mover.LastPos = pos;
                BotOwner.Mover.LastTimePosChanged = Time.time;
                return false;
            }
            else
            {
                TrySolveStuck();
            }
            return true;
        }

        protected virtual void TrySolveStuck()
        {
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

        protected virtual bool EndHoldPosition()
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

        protected virtual bool CanSearchEnemy()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            return goalEnemy == null || !WasHitRecently(10f) && !goalEnemy.IsVisible && !goalEnemy.CanShoot && goalEnemy.CanISearch && BotOwner.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Attack) && BotOwner.Memory.LastEnemyVisionOld(LocalBotSettingsProviderClass.Core.COVER_SECONDS_AFTER_LOSE_VISION);
        }

        protected virtual bool ProtectCareKill()
        {
            return (Time.time - GetEnemyLastSeenTime()) < 10f;
        }

        protected virtual bool ProtectWantKill()
        {
            return (Time.time - BotOwner.BotsGroup.EnemyLastSeenTimeReal) < BotOwner.Settings.FileSettings.Mind.ATTACK_ENEMY_IF_PROTECT_DELTA_LAST_TIME_SEEN;
        }

        protected virtual float GetEnemyLastSeenTime()
        {
            if (BotOwner.Settings.FileSettings.Mind.PROTECT_TIME_REAL)
            {
                return BotOwner.BotsGroup.EnemyLastSeenTimeReal;
            }
            return BotOwner.BotsGroup.EnemyLastSeenTimeSence;
        }

        protected virtual CustomNavigationPoint FollowerCheckData()
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
            var coverSearchData = new CoverSearchData(leadPos, BotOwner.CoverSearchInfo, coverShootType, LocalBotSettingsProviderClass.Core.START_DIST_TO_COV, 0f, CoverSearchType.closerToSelectedPoint, shootPointClass, null, new Vector3?(leadPos), ECheckSHootHide.shootAndHide, new CoverSearchDefenceDataClass(0f), PointsArrayType.byShootType, true);
            return BotOwner.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);
        }

        protected virtual bool ShouldEndPatrol()
        {
            if (BotOwner.PeaceLook.HaveActions())
            {
                return true;
            }

            return false;
        }

        protected virtual bool IsDogFighting()
        {
            return BotOwner.DogFight.DogFightState > BotDogFightStatus.none;
        }

        protected virtual bool EndGoToLootTarget()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (!McsBotPlayerData.IsTaskRunning && !McsBotPlayerData.IsLooting)
            {
                _nextLootingCheckTime = Time.time + LOOTING_FINNISHED_COLDDOWN;
                return true;
            }
            return false;
        }

        protected virtual bool ShouldShootImmediately()
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

        protected virtual bool IsShootFromCoverConditionAllFine()
        {
            if (!BotOwner.Memory.IsInCover)
            {
                return false;
            }
            bool flag;
            if (!BotOwner.LookSensor.EnoughDistToShoot(out flag))
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

        protected virtual bool GetCrossPoint(EnemyInfo enemy)
        {
            var nearestDoor = BotOwner.NearDoorData.GetNearestDoor();
            if (nearestDoor == null)
            {
                return false;
            }
            var position = BotOwner.Transform.position;
            var currPosition = enemy.CurrPosition;
            var gclass = new GClass365(position, currPosition);
            var vector = nearestDoor.SegmentOpen.b - nearestDoor.SegmentOpen.a;
            var vector2 = nearestDoor.SegmentOpen.a - vector * 0.1f;
            var vector3 = nearestDoor.SegmentOpen.b + vector * 0.1f;
            return GClass369.GetCrossPoint(gclass.a, gclass.b, vector2, vector3) != null;
        }

        protected virtual bool CannotSeeEnemy(EnemyInfo info)
        {
            if (info == null)
            {
                return false;
            }
            var vector = info.EnemyLastPositionReal + Vector3.up * 1.6f;
            return !Physics.Linecast(BotOwner.WeaponRoot.position, vector, out var raycastHit, LayerMaskClass.HighPolyWithTerrainMask);
        }

        protected virtual bool CanShootNow()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            return goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible;
        }

        protected virtual bool ShootNow()
        {
            return BotOwner.Memory.GoalEnemy.CanShoot && BotOwner.Memory.GoalEnemy.IsVisible;
        }

        protected virtual bool EndShootFromPlace()
        {
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

        protected virtual bool EndShootFromCover()
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

        protected virtual bool EndGoToEnemy()
        {
            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
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

        protected virtual bool IsEnemyPosLost()
        {
            if (Time.time - BotOwner.Memory.LastEnemyTimeSeen > 20f)
            {
                BotOwner.Memory.GoalEnemy = null;
                return true;
            }
            return false;
        }

        protected virtual bool EndShootToSmoke()
        {
            if (!BotOwner.SmokeGrenade.ShallShoot())
            {
                return true;
            }
            return false;
        }

        protected virtual bool EndRunToEnemy()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (McsBotPlayerData.HasDecision(EDecision.ShouldRegroup))
            {
                return true;
            }

            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
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

        protected virtual bool EndShootFromStationary()
        {
            if (BotOwner.Medecine.FirstAid.Have2Do)
            {
                return true;
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
            if (WasHitRecently(4f))
            {
                return true;
            }
            if (!curLink.IsFree(BotOwner.Id))
            {
                return true;
            }
            if (BotOwner.SuppressStationary.CurUsingLogic.IsReady() && BotOwner.SuppressStationary.CurUsingLogic.CanStartSupressEnemy(BotOwner.Memory.GoalEnemy))
            {
                return true;
            }
            return false;
        }

        protected virtual bool EndGoToExfiltrationPoint()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (Time.time - BotOwner.Mover.LastTimePosChanged > 30f)
            {
                BotOwner.StopMove();
                BotOwner.Mover.AllowTeleport();
                BotOwner.GetPlayer.Teleport(BotOwner.PatrollingData.ExfiltrationData.CachedExfiltrationPoint.Position, true);
                var playerPosition = McsBotPlayerData.Player.Position;
                BotOwner.Mover.LastGoodCastPoint = BotOwner.Mover.PrevSuccessLinkedFrom_1 = BotOwner.Mover.PrevLinkPos = BotOwner.Mover.PositionOnWayInner = playerPosition;
                BotOwner.Mover.LastGoodCastPointTime = Time.time;
                BotOwner.Mover.PrevPosLinkedTime_1 = 0f;
                BotOwner.Mover.SetPlayerToNavMesh(playerPosition);
                BotOwner.Mover.RecalcWay();
                BotOwner.Mover.Pause = true;
                return true;
            }
            return false;
        }

        protected virtual bool IsWannaLeave()
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

        protected virtual bool EndMeleeAttack()
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

        protected virtual EquipmentSlot CheckWeaponSwitch()
        {
            var weaponManager = BotOwner.WeaponManager;
            if (weaponManager == null || weaponManager.Selector == null)
            {
                return EquipmentSlot.FirstPrimaryWeapon;
            }

            if (_nextWeaponSwitchTime > Time.time)
            {
                return weaponManager.Selector.EquipmentSlot;
            }

            weaponManager.Selector.UpdateWeaponsList();
            var targetSlot = DetermineWeaponSlotByAmmo(weaponManager);
            if (targetSlot != weaponManager.Selector.EquipmentSlot)
            {
                TryChangeWeaponSlot(targetSlot);
                _nextWeaponSwitchTime = Time.time + WEAPON_SWITCH_COOLDOWN;
            }

            return targetSlot;
        }

        protected virtual void TryChangeWeaponSlot(EquipmentSlot slot)
        {
            var weaponManager = BotOwner.WeaponManager;
            if (weaponManager?.Selector == null)
            {
                return;
            }

            switch (slot)
            {
                case EquipmentSlot.FirstPrimaryWeapon:
                    weaponManager.Selector.ChangeToMain();
                    break;
                case EquipmentSlot.SecondPrimaryWeapon:
                    weaponManager.Selector.ChangeToSecond();
                    break;
                case EquipmentSlot.Holster:
                    weaponManager.Selector.TryChangeToSlot(slot, false);
                    break;
                case EquipmentSlot.Scabbard:
                    if (weaponManager.Selector.CanChangeToMeleeWeapons)
                    {
                        weaponManager.Selector.ChangeToMelee();
                    }
                    break;
            }
        }

        protected virtual bool HasAmmoOrBackupAmmo(EquipmentSlot slot)
        {
            var equipment = BotOwner.GetPlayer.InventoryController.Inventory.Equipment;

            if (!HasWeaponInSlot(equipment, slot))
            {
                return false;
            }

            var item = equipment.GetSlot(slot).ContainedItem;
            if (item is not Weapon weapon)
            {
                return false;
            }

            var magazineSlot = weapon.GetMagazineSlot();
            if (magazineSlot?.ContainedItem is MagazineItemClass magazine)
            {
                if (magazine.Count > 0)
                {
                    return true;
                }
            }

            if (weapon.ChamberAmmoCount > 0)
            {
                return true;
            }

            return HasBackupAmmo(weapon);
        }

        protected virtual bool HasBackupAmmo(Weapon weapon)
        {
            var player = BotOwner.GetPlayer;
            var inventoryController = player.InventoryController;

            var currentMagazine = weapon.GetCurrentMagazine();
            var magazineSlot = weapon.GetMagazineSlot();

            if (magazineSlot != null)
            {
                var preallocatedMagList = new List<MagazineItemClass>();
                inventoryController.GetReachableItemsOfTypeNonAlloc(preallocatedMagList, null);

                var hasUnusedMagazine = false;

                foreach (var mag in preallocatedMagList)
                {
                    if (mag == currentMagazine)
                    {
                        continue;
                    }

                    if (magazineSlot.CanAccept(mag))
                    {
                        hasUnusedMagazine = true;
                        if (mag.Count > 0)
                        {
                            return true;
                        }
                    }
                }

                if (hasUnusedMagazine)
                {
                    if (HasLooseAmmoForWeapon(weapon))
                    {
                        return true;
                    }
                }
            }

            if (currentMagazine == null)
            {
                return HasLooseAmmoForWeapon(weapon);
            }

            return false;
        }

        protected virtual bool HasLooseAmmoForWeapon(Weapon weapon)
        {
            var player = BotOwner.GetPlayer;
            var inventoryController = player.InventoryController;

            var chamberSlot = weapon.HasChambers ? weapon.Chambers[0] : null;
            var preallocatedAmmoList = new List<AmmoItemClass>();
            inventoryController.GetAcceptableItemsNonAlloc(
                BotReload.AvailableEquipmentSlots,
                preallocatedAmmoList,
                null,
                null
            );

            foreach (var ammo in preallocatedAmmoList)
            {
                if (ammo.StackObjectsCount > 0)
                {
                    if (chamberSlot != null && chamberSlot.CanAccept(ammo))
                    {
                        return true;
                    }

                    if (weapon.GetCurrentMagazine() != null)
                    {
                        var currentMag = weapon.GetCurrentMagazine();
                        if (currentMag.Cartridges.Filters.CheckItemFilter(ammo))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected virtual EquipmentSlot DetermineWeaponSlotByAmmo(BotWeaponManager weaponManager)
        {
            var equipment = BotOwner.GetPlayer.InventoryController.Inventory.Equipment;

            if (HasWeaponInSlot(equipment, EquipmentSlot.FirstPrimaryWeapon))
            {
                if (HasAmmoOrBackupAmmo(EquipmentSlot.FirstPrimaryWeapon))
                {
                    return EquipmentSlot.FirstPrimaryWeapon;
                }
            }

            if (HasWeaponInSlot(equipment, EquipmentSlot.SecondPrimaryWeapon))
            {
                if (HasAmmoOrBackupAmmo(EquipmentSlot.SecondPrimaryWeapon))
                {
                    return EquipmentSlot.SecondPrimaryWeapon;
                }
            }

            if (HasWeaponInSlot(equipment, EquipmentSlot.Holster))
            {
                if (HasAmmoOrBackupAmmo(EquipmentSlot.Holster))
                {
                    return EquipmentSlot.Holster;
                }
            }

            if (HasKnifeInSlot(equipment, EquipmentSlot.Scabbard))
            {
                return EquipmentSlot.Scabbard;
            }

            return weaponManager.Selector.EquipmentSlot;
        }

        protected virtual bool HasWeaponInSlot(InventoryEquipment equipment, EquipmentSlot slot)
        {
            if (equipment == null)
            {
                return false;
            }

            Item item = equipment.GetSlot(slot).ContainedItem;
            return item is Weapon;
        }

        protected virtual bool HasKnifeInSlot(InventoryEquipment equipment, EquipmentSlot slot)
        {
            if (equipment == null)
            {
                return false;
            }

            Item item = equipment.GetSlot(slot).ContainedItem;
            return item is KnifeItemClass;
        }

        protected virtual bool ShouldUseMeleeAttack()
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
            // MiyakoCarryServicePlugin.Logger.LogWarning($"目标武器类型: {targetSlot}");
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
        protected virtual bool ShouldTryVault()
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

            if (Time.time - BotOwner.Mover.LastTimePosChanged < 3f)
            {
                return false;
            }

            return true;
        }

        protected virtual bool TryVault()
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

        protected virtual bool CheckForVaultableObstacle()
        {
            var startPosition = BotOwner.GetPlayer.WeaponRoot.position;
            var lookDirection = BotOwner.GetPlayer.LookDirection.normalized;
            var endPosition = startPosition + lookDirection * SPHERECAST_DISTANCE;

            startPosition.y += 0.33f;
            endPosition.y += 0.33f;

            if (Physics.SphereCast(startPosition, SPHERECAST_RADIUS, lookDirection, out RaycastHit hit, SPHERECAST_DISTANCE, LayerMaskClass.PlayerStaticCollisionsMask))
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

        protected virtual void UpdateLeadNearMoveTarget(Vector3? leadPos, out float nextUpdateTime)
        {
            if (!leadPos.HasValue)
            {
                nextUpdateTime = 1f;
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

            if (!CanGetPathToRun(BotOwner.Position, nearPos.Value, McsBotPlayerData, out Vector3[] corners))
            {
                nextUpdateTime = 0.25f;
                return;
            }

            var newMoveTarget = GetPointAlongPathAtDistance(corners, 15f);
            _currentMoveTarget = newMoveTarget;
            nextUpdateTime = 1f;
        }

        protected virtual void UpdateEscortMoveTarget(Vector3? escortPos, out float nextUpdateTime)
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

            if (!CanGetPathToRun(predictedPos, escortPos.Value, McsBotPlayerData, out Vector3[] corners))
            {
                nextUpdateTime = 0.25f;
                return;
            }

            var newMoveTarget = GetPointAlongPathAtDistance(corners, 15f);
            _currentMoveTarget = newMoveTarget;
            nextUpdateTime = 0.2f;
        }

        protected virtual void UpdateCommonMoveTarget(Vector3? targetPos, out float nextUpdateTime)
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

            if (!CanGetPathToRun(predictedPos, targetPos.Value, McsBotPlayerData, out Vector3[] corners))
            {
                nextUpdateTime = 0.25f;
                return;
            }

            var newMoveTarget = GetPointAlongPathAtDistance(corners, 15f);
            _currentMoveTarget = newMoveTarget;
            nextUpdateTime = 1f;
        }

        protected virtual bool CanGetPathToRun(Vector3 startPos, Vector3 targetPos, McsBotPlayerData mcsBotPlayerData, out Vector3[] corners)
        {
            var navMeshPath = new NavMeshPath();
            NavMesh.CalculatePath(startPos, targetPos, -1, navMeshPath);
            var flag = false;

            var sqrDistanceToTarget = startPos.McsSqrDistance(targetPos);
            var sampleRadius = sqrDistanceToTarget > 50f * 50f ? 5f : 1f;

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
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Negative,
                    });
                    return _lastCanRunResult;
                }

                corners = _lastCalcCorners;
                return _lastCanRunResult;
            }

            _currentMoveRetries = 0;
            _lastCalcCorners = navMeshPath.corners;
            corners = _lastCalcCorners;
            _lastCanRunResult = true;
            return _lastCanRunResult;
        }

        protected virtual Vector3 GetPointAlongPathAtDistance(Vector3[] corners, float distance)
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

        protected virtual void CheckDanger(bool deavtivatingMines = true)
        {
            if (deavtivatingMines)
            {
                BotOwner.BewarePlantedMine.Update();
            }

            BotOwner.BotAvoidDangerPlaces.Update();
        }

        protected virtual bool EndDeactivateMine()
        {
            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (!BotOwner.BewarePlantedMine.CanDeactivate())
            {
                return true;
            }

            return false;
        }

        protected virtual bool EndRunAwayGrenade()
        {
            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
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

        protected virtual bool EndRunAwayArtillery()
        {
            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
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

        protected virtual bool EndRunAwayBTR()
        {
            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
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

        protected virtual bool EndFlashed()
        {
            if (!BotOwner.FlashGrenade.IsFlashed)
            {
                return true;
            }

            return false;
        }

        protected virtual bool EndGoToExcuteProxyAction()
        {
            if (McsBotPlayerData == null)
            {
                return true;
            }

            if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
            {
                CheckStuck();
            }

            if (!McsBotPlayerData.HasDecision([EDecision.ShouldInteractionProxyAction, EDecision.ShouldLootProxyAction, EDecision.ShouldQuestProxyAction]))
            {
                return true;
            }
            return false;
        }
    }
}