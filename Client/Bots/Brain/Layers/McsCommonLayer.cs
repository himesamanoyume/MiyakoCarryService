
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class McsCommonLayer : McsBaseLayer<McsCommonLayer>
    {
        private float _holdPositionTime = Time.time;
        private float _goToCoverTime = Time.time;
        private CustomNavigationPoint _currentNavigationPoint = null;
        private bool _isInCover = false;
        private bool _haveCoverToShoot = false;
        private float _closeBossDistance = 15f;
        private bool _isHolding = false;
        private float _lastHoldTime = Time.time;

        public McsCommonLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            InitActionMap();
        }

        protected override void InitActionMap()
        {
            _endActionMap = new()
            {
                { typeof(GoToCoverPointLogic), EndGoToCoverPoint },
                { typeof(HealLogic), EndHeal },
                { typeof(RunToCoverLogic), EndRunToCover },
                { typeof(SimplePatrolLogic), EndSimplePatrol },
                { typeof(HoldPositionLogic), EndHoldPosition },
                { typeof(GoToPointLogic), EndGoToPoint },
                { typeof(AttackMovingLogic), EndAttackMoving },
                { typeof(GoToLootTargetLogic), EndLootingTarget }
            };
        }

        public override Action GetNextAction()
        {
            try
            {
                // 检查老板生命值状态并且检测老板没有医疗物品，如果老板健康不行但是有医疗物品则无视
                if (false && false)
                {
                    // 根据老板的受伤状态再检查自己是否没有医疗物品
                    if (false && !BotOwner.Medecine.FirstAid.HaveSmth2Use)
                    {
                        // 尝试寻找周围的医疗物品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }

                    // 根据老板的受伤状态再检查自己是否没有手术包
                    if (false && !BotOwner.Medecine.SurgicalKit.HaveSmth2Use)
                    {
                        // 尝试寻找周围的手术包
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }

                    // 跑到老板旁边扔出医疗物品
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                }

                // 刷新自身受伤状态
                BotOwner.Medecine.GetDamaged();
                // 是否受到了非部位摧毁伤害
                if (BotOwner.Medecine.FirstAid.Damaged)
                {
                    // 是否没有医疗物品
                    if (!BotOwner.Medecine.FirstAid.HaveSmth2Use)
                    {
                        // 尝试寻找周围的医疗物品
                        // return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    else
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal:RunToCoverLogic1");
                        }
                    }
                }

                // 是否受到了部位摧毁伤害
                if (BotOwner.Medecine.SurgicalKit.Damaged)
                {
                    // 是否没有手术包
                    if (!BotOwner.Medecine.SurgicalKit.HaveSmth2Use)
                    {
                        // 尝试寻找周围的手术包
                        // return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    else
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal:RunToCoverLogic2");
                        }
                    }
                }

                // 老板健康无大碍，且医疗物品和掩体也都准备就绪后，才治疗自己
                if (BotOwner.Medecine.FirstAid.Damaged || BotOwner.Medecine.SurgicalKit.Damaged)
                {
                    return new Action(typeof(HealLogic), "Mcs:first aid");
                }

                // 检查老板的吃喝状态是否低于阈值且老板身上没有吃喝
                if (false && false)
                {
                    // 是否是缺能量且自身没有能补充能量的食品
                    if (false && false)
                    {
                        // 尝试寻找周围的能补充能量的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    // 是否是缺水且自身没有能补充水的食品
                    else if (false && false)
                    {
                        // 尝试寻找周围的能补充水的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }

                    // 老板缺能量且自身有能补充能量的食品
                    if (false && false)
                    {
                        // 跑到老板旁边扔出能补充能量的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    // 老板缺水且自身有能补充水的食品
                    else if (false && false)
                    {
                        // 跑到老板旁边扔出能补充水的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                }

                // 老板吃喝无大碍
                if (false)
                {
                    // 自身是否是缺能量且自身没有能补充能量的食品
                    if (false && false)
                    {
                        // 尝试寻找周围的能补充能量的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    // 自身是否是缺水且自身没有能补充水的食品
                    else if (false && false)
                    {
                        // 尝试寻找周围的能补充水的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }

                    // 自身缺能量且自身有能补充能量的食品
                    if (false && false)
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal");
                        }
                    }
                    // 自身缺水且自身有能补充水的食品
                    else if (false && false)
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal");
                        }
                    }

                    // 已位于掩体
                    if (BotOwner.Memory.IsInCover)
                    {
                        // 缺能量
                        if (false)
                        {
                            // 吃能补充能量的食品
                            return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                        }
                        // 缺水分
                        else if (false)
                        {
                            // 吃能补充水分的食品
                            return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                        }
                    }
                }

                // 检查当前包内战利品价值是否超过阈值，且老板身上是否还有空位
                if (false && false)
                {
                    // 跑到老板旁边扔出背包
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                }

                // 当护航目前身上没穿着背包时，则认为当前正在上缴贡品
                if (false)
                {
                    // 检测老板与自身的距离，若超过一定距离则认为老板看不上剩下的物品了，于是重新拾取背包
                    if (false)
                    {
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                }

                // if (McsBotPlayerData != null)
                // {
                //     // 检测周围是否有符合条件的战利品
                //     if (McsBotPlayerData.LootingTarget != null)
                //     {
                //         // 尝试去拿战利品
                //         return new Action(typeof(GoToLootTargetLogic), "Mcs:going to loot target");
                //     }

                //     // 取消当前锁定的目标战利品
                //     McsBotPlayerData.UnlockLootingTarget();
                // }

                // 检查与老板之间的距离，若超过一定距离则需要跑到老板附近
                var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
                if (mcsLeadPlayerPos == null)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:bossPosNull");
                }

                if ((BotOwner.Position - mcsLeadPlayerPos).sqrMagnitude > _closeBossDistance)
                {
                    TryFindCover(mcsLeadPlayerPos);
                    if (_isInCover)
                    {
                        BotOwner.Memory.BotCurrentCoverInfo.SetCover(_currentNavigationPoint, true);
                        if (BotOwner.CanSprintPlayer)
                        {
                            return new Action(typeof(AttackMovingLogic), "Mcs:sDistCloseB:AttackMovingLogic");
                        }
                        return new Action(typeof(RunToCoverLogic), "Mcs:sDistCloseB:RunToCoverLogic1");
                    }
                    else
                    {
                        var xOffset = GClass856.Random(0.5f, 2.5f) * GClass856.RandomSing();
                        var zOffset = GClass856.Random(0.5f, 2.5f) * GClass856.RandomSing();
                        var newPos = mcsLeadPlayerPos + new Vector3(xOffset, 0, zOffset);
                        var closestPoint = BotOwner.Covers.GetClosestPoint(newPos);
                        if (closestPoint != null)
                        {
                            BotOwner.Memory.BotCurrentCoverInfo.SetCover(closestPoint, true);
                            return new Action(typeof(RunToCoverLogic), "Mcs:sDistCloseB:RunToCoverLogic2");
                        }
                        if (Time.time - _goToCoverTime > 5f && NavMesh.SamplePosition(newPos, out var navMeshHit, 5f, -1))
                        {
                            BotOwner.GoToSomePointData.SetPoint(navMeshHit.position);
                            return new Action(typeof(GoToPointLogic), "Mcs:sDistCloseB:GoToPointLogic");
                        }
                        if (BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(HoldPositionLogic), "Mcs:sDistCloseB:HoldPositionLogic");
                        }
                        return new Action(typeof(RunToCoverLogic), "Mcs:sDistCloseB:RunToCoverLogic");
                    }
                }
                else
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:distToBoss");
                }
                // end
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:Exception");
            }
        }

        public override bool IsActive()
        {
            if (IsMcsBotPlayer)
            {
                if (BotOwner.Memory.HaveEnemy || BotOwner.Memory.IsUnderFire)
                {
                    BotOwner.PriorityAxeTarget.FindTarget();
                    BotOwner.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Attack);
                    return false;
                }
                else
                {
                    BotOwner.PriorityAxeTarget.FindTarget();
                    BotOwner.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Protect);
                }
                return true;
            }
            return false;
        }

        private void TryFindCover(Vector3 mcsLeadPlayerPos)
        {
            if (_goToCoverTime < Time.time)
            {
                _goToCoverTime = Time.time + 1f;
                var coverSearchData = new CoverSearchData(mcsLeadPlayerPos, BotOwner.CoverSearchInfo, CoverShootType.hide, _closeBossDistance, 0f, CoverSearchType.closerToSelectedPoint, null, null, new Vector3?(mcsLeadPlayerPos), ECheckSHootHide.shootAndHide, new CoverSearchDefenceDataClass(0f), PointsArrayType.byShootType, true, null, null, "Default");
                _currentNavigationPoint = BotOwner.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);
                if (_currentNavigationPoint != null)
                {
                    if ((mcsLeadPlayerPos - _currentNavigationPoint.Position).sqrMagnitude < _closeBossDistance && !_currentNavigationPoint.IsSpotted)
                    {
                        _isInCover = true;
                        return;
                    }
                }
                _isInCover = false;
            }
        }

        private bool WasHitRecently(float timeframe)
        {
            return (Time.time - BotOwner.Memory.LastTimeHit) < timeframe;
        }

        private bool EndEatDrink()
        {
            return true;
        }

        private bool EndGoToCoverPoint()
        {
            var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
            if (mcsLeadPlayerPos != null)
            {
                TryFindCover(mcsLeadPlayerPos);
                UpdateCoverToShoot();
                if (!_isInCover && !_haveCoverToShoot)
                {
                    return true;
                }
            }

            // If we're in cover, end going to cover
            if (BotOwner.Memory.IsInCover)
            {
                return true;
            }

            // Not sure why this would exit the GoToCover state
            EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.IsVisible && goalEnemy.CanShoot)
            {
                return true;
            }

            return false;
        }

        private bool EndHeal()
        {
            // If we no longer have first aid to do, stop healing
            if (!BotOwner.Medecine.FirstAid.Have2Do)
            {
                return true;
            }

            return false;
        }

        private bool EndRunToCover()
        {
            var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
            if (mcsLeadPlayerPos != null)
            {
                TryFindCover(mcsLeadPlayerPos);
                UpdateCoverToShoot();
                if (!_isInCover && !_haveCoverToShoot)
                {
                    return true;
                }
            }

            // If we're in cover, end running to cover
            if (BotOwner.Memory.IsInCover)
            {
                return true;
            }

            // If we can't sprint any more, end running to cover
            if (!BotOwner.CanSprintPlayer)
            {
                return true;
            }

            // If we've started dogfighting, stop running for cover
            if (IsDogFighting())
            {
                return true;
            }

            // If our cover point has been spotted, stop running to it
            if (BotOwner.Memory.CurCustomCoverPoint != null && BotOwner.Memory.CurCustomCoverPoint.IsSpotted)
            {
                return true;
            }

            return false;
        }

        private bool EndSimplePatrol()
        {
            // If we should generally end the patrol, due so
            if (ShouldEndPatrol())
            {
                return true;
            }

            // If our patrol is now a reserved patrol, stop doing a simple patrol
            if (BotOwner.PatrollingData.Way.PatrolType == PatrolType.reserved)
            {
                return true;
            }

            // If we have a boss, and aren't a boss ourselves, stop doing a simple patrol
            if (BotOwner.BotFollower.HaveBoss && !BotOwner.Boss.IamBoss)
            {
                return true;
            }

            return false;
        }

        private bool EndGoToPoint()
        {
            if (BotOwner.GoToSomePointData.IsCome())
            {
                return true;
            }
            return false;
        }

        private bool EndAttackMoving()
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
            return false;
        }

        private bool EndHoldPosition()
        {
            UpdateCoverToShoot();
            var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
            if ((BotOwner.Position - mcsLeadPlayerPos).sqrMagnitude > _closeBossDistance)
            {
                return true;
            }
            if (_haveCoverToShoot && ProtectWantKill() && ProtectCareKill())
            {
                return true;
            }

            if (IsHolding())
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
                if (goalEnemy.IsVisible && goalEnemy.Distance < 75f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanSearchEnemy()
        {
            var goalEnemy = BotOwner.Memory.GoalEnemy;
            return goalEnemy == null || !WasHitRecently(10f) && !goalEnemy.IsVisible && !goalEnemy.CanShoot && goalEnemy.CanISearch && BotOwner.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Attack) && BotOwner.Memory.LastEnemyVisionOld(LocalBotSettingsProviderClass.Core.COVER_SECONDS_AFTER_LOSE_VISION);
        }

        private bool IsHolding()
        {
            if (!_isHolding)
            {
                return false;
            }
            if (_lastHoldTime < Time.time)
            {
                _isHolding = false;
                return true;
            }
            return false;
        }

        private void UpdateCoverToShoot()
        {
            if (_holdPositionTime < Time.time)
            {
                _holdPositionTime = Time.time + 1f;
                Vector3 bossPos;
                if (BotOwner.BotFollower.HaveBoss)
                {
                    bossPos = McsBotPlayerData.BossPlayer.Position;
                }
                else
                {
                    bossPos = BotOwner.Position;
                }
                _currentNavigationPoint = FollowerCheckData();
                if (_currentNavigationPoint != null && _currentNavigationPoint.IsFreeById(BotOwner.Id) && !_currentNavigationPoint.IsSpotted)
                {
                    var sqrMagnitude = (bossPos - _currentNavigationPoint.Position).sqrMagnitude;
                    if (sqrMagnitude >= 75f)
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

        private bool ProtectCareKill()
        {
            return (Time.time - GetEnemyLastSeenTime()) < 10f;
        }

        private bool ProtectWantKill()
        {
            return (Time.time - BotOwner.BotsGroup.EnemyLastSeenTimeReal) < BotOwner.Settings.FileSettings.Mind.ATTACK_ENEMY_IF_PROTECT_DELTA_LAST_TIME_SEEN;
        }

        private float GetEnemyLastSeenTime()
        {
            if (BotOwner.Settings.FileSettings.Mind.PROTECT_TIME_REAL)
            {
                return BotOwner.BotsGroup.EnemyLastSeenTimeReal;
            }
            return BotOwner.BotsGroup.EnemyLastSeenTimeSence;
        }

        private CustomNavigationPoint FollowerCheckData()
        {
            Vector3 bossPos;
            if (BotOwner.BotFollower.HaveBoss)
            {
                bossPos = McsBotPlayerData.BossPlayer.Position;
            }
            else
            {
                bossPos = BotOwner.Position;
            }
            var shootPointClass = BotOwner.CurrentEnemyTargetPosition(true);
            var coverShootType = CoverShootType.shoot;
            if (shootPointClass == null)
            {
                coverShootType = CoverShootType.hide;
            }
            var coverSearchData = new CoverSearchData(bossPos, BotOwner.CoverSearchInfo, coverShootType, LocalBotSettingsProviderClass.Core.START_DIST_TO_COV, 0f, CoverSearchType.closerToSelectedPoint, shootPointClass, null, new Vector3?(bossPos), ECheckSHootHide.shootAndHide, new CoverSearchDefenceDataClass(0f), PointsArrayType.byShootType, true);
            return BotOwner.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);
        }

        private Vector3 GetMcsLeadPlayerPos()
        {
            if (BotOwner.BotFollower.HaveBoss)
            {
                if (McsBotPlayerData == null)
                {
                    return BotOwner.Position;
                }

                return McsBotPlayerData.BossPlayer.Position;
            }
            else
            {
                return BotOwner.Position;
            }
        }

        private bool ShouldEndPatrol()
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

        private bool IsDogFighting()
        {
            return BotOwner.DogFight.DogFightState > BotDogFightStatus.none;
        }

        private bool EndLootingTarget()
        {
            return !McsBotPlayerData.IsRunningCoroutine && !McsBotPlayerData.IsLooting;
        }
    }
}