using System;
using System.Collections.Generic;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public abstract class McsBaseLayer<T>(BotOwner botOwner, int priority) : CustomLayer(botOwner, priority) where T : McsBaseLayer<T>
    {
        private bool? _isMcsBotPlayer = null;

        public bool IsMcsBotPlayer => _isMcsBotPlayer ??= BotOwner.IsMcsBotPlayer;
        protected Dictionary<Type, Func<bool>> _endActionMap;
        protected bool _haveCoverToShoot = false;
        protected float _lastHoldPositionTime = Time.time;
        protected float _goToCoverTime = Time.time;
        protected CustomNavigationPoint _currentNavigationPoint = null;
        protected float _closeLeadDistance = 20f;
        protected float _lastPatrolTime = Time.time;
        protected float _lastGoToPointTime = Time.time;
        protected float _lastShootTime = Time.time;

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

        protected SubTitleMgr SubTitleMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SubTitleMgr>();
            }
        }

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
                { typeof(GoToLootTargetLogic), EndLootingTarget },
                { typeof(ShootFromPlaceLogic), EndShootFromPlace },
                { typeof(ShootFromCoverLogic), EndShootFromCover },
                { typeof(ShootToSmokeLogic), EndShootToSmoke },
                { typeof(ShootFromStationaryLogic), EndShootFromStationary },
                { typeof(RunToEnemyLogic), EndRunToEnemy },
                { typeof(GoToExfiltrationPointNodeLogic), EndGoToExfiltrationPoint },
            };
        }

        protected Vector3 GetMcsLeadPlayerPos()
        {
            if (McsBotPlayerData != null && McsBotPlayerData.LeadPlayer != null && McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
            {
                return McsBotPlayerData.LeadPlayer.Position;
            }
            else if (BotOwner.BotFollower.HaveBoss)
            {
                return BotOwner.BotFollower.BossToFollow.Position;
            }
            else
            {
                if (BotOwner.Position == null)
                {
                    return new();
                }
                return BotOwner.Position;
            }
        }

        protected virtual bool EndHeal()
        {
            if (!BotOwner.Medecine.FirstAid.Have2Do)
            {
                return true;
            }
            return false;
        }

        protected virtual bool EndRunToCover()
        {
            var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
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
                var coverSearchData = new CoverSearchData(mcsLeadPlayerPos, BotOwner.CoverSearchInfo, CoverShootType.hide, _closeLeadDistance, 0f, CoverSearchType.closerToSelectedPoint, null, null, new Vector3?(mcsLeadPlayerPos), ECheckSHootHide.shootAndHide, new CoverSearchDefenceDataClass(0f), PointsArrayType.byShootType, true, null, null, "Default");
                _currentNavigationPoint = BotOwner.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);
                if (_currentNavigationPoint != null)
                {
                    if (mcsLeadPlayerPos.McsSqrDistance(_currentNavigationPoint.Position) < _closeLeadDistance * _closeLeadDistance && !_currentNavigationPoint.IsSpotted)
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

            if (_lastHoldPositionTime < Time.time)
            {
                _lastHoldPositionTime = Time.time + 1f;
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
            var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
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
            if (BotOwner.GoToSomePointData.IsCome())
            {
                if (McsBotPlayerData.ShouldGoToPoint)
                {
                    McsBotPlayerData.ShouldGoToPoint = false;
                    McsBotPlayerData.ShouldHoldPosition = true;
                }
                return true;
            }
            else
            {
                var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
                if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
                {
                    CheckStuck();
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged > 30f && BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= _closeLeadDistance * _closeLeadDistance)
                {
                    if (MiyakoCarryServicePlugin.SAINInstalled)
                    {
                        BotOwner.StopMove();
                        BotOwner.Mover.AllowTeleport();
                        BotOwner.GetPlayer.Teleport(McsBotPlayerData.LeadPlayer.Position, true);
                    }
                    return true;
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged > 6f)
                {
                    if (McsBotPlayerData.ShouldGoToPoint)
                    {
                        McsBotPlayerData.ShouldGoToPoint = false;
                        McsBotPlayerData.ShouldHoldPosition = true;
                    }
                    return true;
                }
                return false;
            }
        }

        protected virtual bool EndGoToProtect()
        {
            if (BotOwner.GoToSomePointData.IsCome())
            {
                return true;
            }
            else
            {
                var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
                if (BotOwner.Mover.LastTimePosChanged + 1f < Time.time)
                {
                    CheckStuck();
                }

                if (Time.time - BotOwner.Mover.LastTimePosChanged > 30f && BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= _closeLeadDistance * _closeLeadDistance)
                {
                    if (MiyakoCarryServicePlugin.SAINInstalled)
                    {
                        BotOwner.StopMove();
                        BotOwner.Mover.AllowTeleport();
                        BotOwner.GetPlayer.Teleport(McsBotPlayerData.LeadPlayer.Position, true);
                    }
                    BotOwner.TalkMsg(EPhraseTrigger.Regroup);
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
            return true;
        }

        protected virtual bool EndHoldPosition()
        {
            UpdateCoverToShoot();
            var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
            if (BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) > _closeLeadDistance * _closeLeadDistance)
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
            if (McsBotPlayerData.LeadPlayer != null && McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
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
            // if (!IsBossOrFollower())
            // {
            //     if (BotOwner.EatDrinkData.HaveActions())
            //     {
            //         return true;
            //     }
            // }

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

        protected virtual bool EndLootingTarget()
        {
            return !McsBotPlayerData.IsRunningCoroutine && !McsBotPlayerData.IsLooting;
        }

        protected virtual bool ShouldShootImmediately()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            var flag = ((goalEnemy != null && goalEnemy.Distance < BotOwner.Settings.FileSettings.Shoot.SHOOT_IMMEDIATELY_DIST) || BotOwner.BotsGroup.AnyBodyShootImmediately) && goalEnemy.CanShoot && Time.time - goalEnemy.AddTime < 5f;
            var isActive = BotOwner.WeaponManager.UnderbarrelLauncherController.IsActive;
            BotOwner.BotsGroup.AnyBodyShootImmediately = flag || isActive;
            return BotOwner.BotsGroup.AnyBodyShootImmediately;
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
            if (_lastShootTime < Time.time)
            {
                _lastShootTime = Time.time + 3f;
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
            // if (this.method_22())
            // {
            //     return false;
            // }
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
            return true;
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
    }
}