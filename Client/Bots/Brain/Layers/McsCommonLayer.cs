
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class McsCommonLayer : McsBaseLayer<McsCommonLayer>
    {
        // private float _nextReloadTime;
        private float _holdPositionTime = Time.time;
        private CustomNavigationPoint _pointToShoot = null;
        private bool _haveCoverToShoot = false;
        private float _tooCloseBossDistance = 5f;
        private bool _isHolding = false;
        private float _lastHoldTime = Time.time;
        public McsCommonLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

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
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal");
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
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal");
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

                // 检测周围是否有符合条件的战利品
                if (false)
                {
                    // 尝试去拿战利品
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                }

                // 检查与老板之间的距离，若超过一定距离则需要跑到老板附近
                if (false)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                }

                // if (BotOwner.Medecine.FirstAid.Have2Do || BotOwner.Medecine.SurgicalKit.HaveWork)
                // {
                //     // If we are already in cover, we can heal
                //     if (BotOwner.Memory.IsInCover)
                //     {
                //         return new Action(typeof(HealLogic), "Mcs:first aid");
                //     }

                //     // If we were hit in the last 20 seconds, run to cover before healing
                //     if (WasHitRecently(20f))
                //     {
                //         return new Action(typeof(RunToCoverLogic), "Mcs:goforheal");
                //     }

                //     // Otherwise we can heal
                //     return new Action(typeof(HealLogic), "Mcs:heal now");
                // }

                // // If we're in a smoke grenade, go to a cover point
                // if (BotOwner.SmokeGrenade.IsInSmoke)
                // {
                //     return new Action(typeof(GoToCoverPointLogic), "Mcs:PeaceSmoke");
                // }

                // // 
                // if (BotOwner.PeaceHardAim.HaveActions())
                // {
                //     return new Action(typeof(PeaceHardAimLogic), "Mcs:PeaceHardAi");
                // }

                // // 
                // if (BotOwner.PeaceLook.HaveActions())
                // {
                //     return new Action(typeof(PeaceLookLogic), "Mcs:PeaceLook");
                // }

                // // 
                // if (BotOwner.SecondWeaponData.HaveActions())
                // {
                //     return new Action(typeof(WatchSecondWeaponLogic), "Mcs:Look2ndWeap");
                // }

                // Do some specific things if we aren't a boss or follower
                // if (!IsBossOrFollower())
                // {

                //     // Should we be eating/drinking?
                //     if (BotOwner.EatDrinkData.HaveActions())
                //     {
                //         return new Action(typeof(EatDrinkLogic), "Mcs:EatDrinkDat");
                //     }

                //     // Did a player 手势 to us?
                //     if (BotOwner.Gesture.HaveRequest())
                //     {
                //         return new Action(typeof(GestureLogic), "Mcs:Gesture");
                //     }

                //     // 
                //     if (BotOwner.PeacefulActions.HaveActions())
                //     {
                //         return new Action(typeof(PeacefulLogic), "Mcs:Peaceful");
                //     }
                // }

                // // Get patrolling information
                // BotOwner.PatrollingData.SetTargetMoveSpeed();
                // BotOwner.PatrollingData.PointChooser.ShallChangeWay(false);
                // var patrolWay = GetCurrentPatrolWay();

                // // Reload if we're under 60% ammo, and it's been long enough since our last reload
                // float ammoPercent = BotOwner.WeaponManager.Reload.BulletCount / BotOwner.WeaponManager.Reload.MaxBulletCount;
                // if (ammoPercent < 0.6f && Time.time >= _nextReloadTime)
                // {
                //     _nextReloadTime = Time.time + 30f;
                //     BotOwner.WeaponManager.Reload.TryReload();
                // }

                // // If we have a patrol, it's a reserve patrol, and the bot is allowed on reserve patrols, set the action to alternative patrol
                // if (patrolWay != null && patrolWay.PatrolType == PatrolType.reserved && BotOwner.Settings.FileSettings.Patrol.CAN_CHOOSE_RESERV)
                // {
                //     BotOwner.PatrollingData.ComeToPoint();
                //     return new Action(typeof(AlternativePatrolLogic), "Mcs:RESER");
                // }

                // // If we are not a boss, and we're following a boss, set our action to FollowerPatrol
                // if (!BotOwner.Boss.IamBoss && BotOwner.BotFollower.HaveBoss)
                // {
                //     return new Action(typeof(FollowerPatrolLogic), "Mcs:BossFollow");
                // }

                return new Action(typeof(HoldPositionLogic), "Mcs:distToBoss");
                // return new Action(typeof(McsBotPlayerPatrolLogic), "nothing to do");
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(HoldPositionLogic), "Mcs:distToBoss");
            }
        }

        public override bool IsActive()
        {
            BotOwner.PriorityAxeTarget.FindTarget();
            if (BotOwner.Memory.HaveEnemy || BotOwner.Memory.IsUnderFire)
            {
                return false;
            }

            if (BotOwner.BotFollower.HaveBoss && IsMcsBotPlayer)
            {
                return true;
            }
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            if (CurrentAction == null)
            {
                return true;
            }

            Type currentActionType = CurrentAction.Type;

            // I don't really know a more elegant way to do this condition than direct comparing the types
            if (currentActionType == typeof(AlternativePatrolLogic))
            {
                return EndAlternativePatrol();
            }
            // else if (currentActionType == typeof(EatDrinkLogic))
            // {
            //     return EndEatDrink();
            // }
            else if (currentActionType == typeof(FollowerPatrolLogic))
            {
                return EndFollowerPatrol();
            }
            // else if (currentActionType == typeof(FriendlyTiltLogic))
            // {
            //     return EndFriendlyTilt();
            // }
            // else if (currentActionType == typeof(GestureLogic))
            // {
            //     return EndGesture();
            // }
            else if (currentActionType == typeof(GoToCoverPointLogic))
            {
                return EndGoToCoverPoint();
            }
            else if (currentActionType == typeof(HealLogic))
            {
                return EndHeal();
            }
            // else if (currentActionType == typeof(PeacefulLogic))
            // {
            //     return EndPeaceful();
            // }
            else if (currentActionType == typeof(PeaceHardAimLogic))
            {
                return EndPeaceHardAim();
            }
            else if (currentActionType == typeof(PeaceLookLogic))
            {
                return EndPeaceLook();
            }
            else if (currentActionType == typeof(RunToCoverLogic))
            {
                return EndRunToCover();
            }
            else if (currentActionType == typeof(SimplePatrolLogic))
            {
                return EndSimplePatrol();
            }
            else if (currentActionType == typeof(WatchSecondWeaponLogic))
            {
                return EndWatchSecondWeapon();
            }
            else if (currentActionType == typeof(HoldPositionLogic))
            {
                return EndHoldPosition();
            }

            // If it's not a logic we handle, end it
            return true;
        }

        private PatrolWay GetCurrentPatrolWay()
        {
            // Otherwise, if we don't have a PatrolWay yet, choose one
            if (BotOwner.PatrollingData.Way == null)
            {
                BotOwner.PatrollingData.PointChooser.ChooseStartWay();
            }

            return BotOwner.PatrollingData.Way;
        }

        private bool WasHitRecently(float timeframe)
        {
            return (Time.time - BotOwner.Memory.LastTimeHit) < timeframe;
        }

        private bool EndAlternativePatrol()
        {
            // If we should generally end the patrol, due so
            if (ShouldEndPatrol())
            {
                return true;
            }

            // If we're still patrolling a reserved patrol, don't end
            if (BotOwner.PatrollingData.Way.PatrolType == PatrolType.reserved)
            {
                return false;
            }

            return true;
        }

        private bool EndEatDrink()
        {
            return true;
        }

        private bool EndFollowerPatrol()
        {
            // If we've switched to a reserved patrol, stop our follower patrol
            if (BotOwner.PatrollingData.Way.PatrolType == PatrolType.reserved)
            {
                return true;
            }

            // If we are now a boss, end our follower patrol
            if (BotOwner.Boss.IamBoss)
            {
                return true;
            }

            // If we no longer have a boss, end our follower patrol
            if (!BotOwner.BotFollower.HaveBoss)
            {
                return true;
            }

            return false;
        }

        private bool EndFriendlyTilt()
        {
            return true;
        }

        private bool EndGesture()
        {
            return true;
        }

        private bool EndGoToCoverPoint()
        {
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

        private bool EndPeaceful()
        {
            // If we have peaceful actions to do, end Peaceful
            if (BotOwner.PeacefulActions.HaveActions())
            {
                return false;
            }

            return true;
        }

        private bool EndPeaceHardAim()
        {
            return true;
        }

        private bool EndPeaceLook()
        {
            return true;
        }

        private bool EndRunToCover()
        {
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

        private bool EndHoldPosition()
        {
            UpdateCoverToShoot();
            var myPos = GetMyPos();
            if ((BotOwner.Position - myPos).sqrMagnitude > _tooCloseBossDistance)
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
                if (goalEnemy.IsVisible && goalEnemy.Distance < BotOwner.Settings.FileSettings.Cover.END_HOLD_IF_ENEMY_CLOSE_AND_VISIBLE)
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
                _pointToShoot = FollowerCheckData();
                if (_pointToShoot != null && _pointToShoot.IsFreeById(BotOwner.Id) && !_pointToShoot.IsSpotted)
                {
                    float sqrMagnitude = (bossPos - _pointToShoot.Position).sqrMagnitude;
                    if (sqrMagnitude >= BotOwner.Settings.FileSettings.Boss.MAX_DIST_COVER_BOSS_SQRT)
                    {
                        _haveCoverToShoot = false;
                        return;
                    }
                    if (ProtectCareKill())
                    {
                        bool canIShootToEnemy = _pointToShoot.CanIShootToEnemy;
                        _haveCoverToShoot = canIShootToEnemy;
                    }
                    else
                    {
                        _haveCoverToShoot = true;
                    }
                    if (_haveCoverToShoot && (BotOwner.Memory.CurCustomCoverPoint == null || BotOwner.Memory.CurCustomCoverPoint.Id != _pointToShoot.Id))
                    {
                        BotOwner.Memory.BotCurrentCoverInfo.Spotted();
                        BotOwner.Memory.BotCurrentCoverInfo.SetCover(_pointToShoot, true);
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
            return (Time.time - GetEnemyLastSeenTime()) < BotOwner.Settings.FileSettings.Mind.HOLD_IF_PROTECT_DELTA_LAST_TIME_SEEN;
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
            var coverSerachData = new CoverSearchData(bossPos, BotOwner.CoverSearchInfo, coverShootType, LocalBotSettingsProviderClass.Core.START_DIST_TO_COV, 0f, CoverSearchType.closerToSelectedPoint, shootPointClass, null, new Vector3?(bossPos), ECheckSHootHide.shootAndHide, new CoverSearchDefenceDataClass(0f), PointsArrayType.byShootType, true);
            return BotOwner.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSerachData, true);
        }

        private Vector3 GetMyPos()
        {
            if (BotOwner.BotFollower.BossToFollow != null)
            {
                return BotOwner.BotFollower.BossToFollow.Position;
            }
            return BotOwner.Position;
        }

        private bool EndWatchSecondWeapon()
        {
            // When our second weapon has actions, stop watching it? What?
            if (BotOwner.SecondWeaponData.HaveActions())
            {
                return false;
            }

            return true;
        }

        private bool ShouldEndPatrol()
        {
            // if (!IsBossOrFollower())
            // {
            //     if (BotOwner.EatDrinkData.HaveActions())
            //     {
            //         return true;
            //     }

            //     if (BotOwner.FriendlyTilt.HaveActions())
            //     {
            //         return true;
            //     }

            //     if (BotOwner.Gesture.HaveRequest())
            //     {
            //         return true;
            //     }

            //     if (BotOwner.SecondWeaponData.HaveActions())
            //     {
            //         return true;
            //     }

            //     if (BotOwner.PeacefulActions.HaveActions())
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
    }
}