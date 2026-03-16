
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class McsFightLayer : McsBaseLayer<McsFightLayer>
    {
        public McsFightLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            InitActionMap();
        }

        protected override void InitActionMap()
        {
            _endActionMap = new()
            {
                { typeof(HoldPositionLogic), EndHoldPosition },
                { typeof(SimplePatrolLogic), EndSimplePatrol },
                { typeof(ShootFromPlaceLogic), EndShootFromPlace },
                { typeof(ShootFromCoverLogic), EndShootFromCover },
                { typeof(RunToCoverLogic), EndRunToCover },
                { typeof(AttackMovingLogic), EndAttackMoving },
                { typeof(GoToEnemyLogic), EndGoToEnemy },
                { typeof(HealLogic), EndHeal },
                { typeof(GoToCoverPointLogic), EndGoToCoverPoint },
            };
        }

        public override Action GetNextAction()
        {
            if (ShouldShootImmediately())
            {
                return new Action(typeof(ShootFromPlaceLogic), "Mcs:ShootImmediately");
            }

            if (IsShootFromCoverConditionAllFine())
            {
                return new Action(typeof(ShootFromCoverLogic), "Mcs:ShootFromCover");
            }

            var goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy == null)
            {
                return new Action(typeof(HoldPositionLogic), "Mcs:!HaveEnemy");
            }

            if (BotOwner.NearDoorData.RecentlyClosedDoorCheckTime + 0.3f < Time.time && BotOwner.BotsGroup.EnemyLastSeenTimeReal + 7f >= Time.time && GetCrossPoint(goalEnemy))
            {
                BotOwner.Memory.Spotted(false, null, null);
            }

            try
            {
                var canShoot = goalEnemy.CanShoot;
                var isProtectWantKill = ProtectWantKill();
                var isProtectCareKill = ProtectCareKill();
                UpdateCoverToShoot();
                if (!goalEnemy.IsVisible && BotOwner.SmokeGrenade.ShallShoot())
                {
                    return new Action(typeof(ShootToSmokeLogic), "Mcs:SmokeGrenad");
                }
                else if (_haveCoverToShoot && isProtectWantKill)
                {
                    var canSeeEnemy = CanSeeEnemy(goalEnemy);
                    var canShootNow = CanShootNow();
                    if (canSeeEnemy && !canShootNow && Time.time - goalEnemy.PersonalSeenTime < 3f)
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:canShootLas");
                    }
                    else if (!canShootNow && goalEnemy.Distance > 10f)
                    {
                        BotOwner.Memory.BotCurrentCoverInfo.SetCover(_currentNavigationPoint, true);
                        if (BotOwner.CanSprintPlayer)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goalEnemy.D");
                        }
                        else
                        {
                            return new Action(typeof(AttackMovingLogic), "Mcs:goal.D");
                        }
                    }
                    else if (BotOwner.Memory.IsInCover && BotOwner.Memory.CurCustomCoverPoint.Id == _currentNavigationPoint.Id)
                    {
                        return new Action(typeof(ShootFromCoverLogic), "Mcs:.Memor1");
                    }
                    else
                    {
                        return new Action(typeof(AttackMovingLogic), "Mcs:.Memor2");
                    }
                }
                else
                {
                    var safeFire = false;
                    if (canShoot)
                    {
                        var closestFriend = BotOwner.Covers.GetClosestFriend(out var num);
                        safeFire = num >= LocalBotSettingsProviderClass.Core.MIN_DIST_CLOSE_DEF || !(closestFriend != null) || closestFriend.Id > BotOwner.Id;
                    }
                    if (safeFire)
                    {
                        if (goalEnemy.IsVisible)
                        {
                            return new Action(typeof(ShootFromPlaceLogic), "Mcs:goalEnemy.V");
                        }
                        else if (!WasHitRecently(BotOwner.Settings.FileSettings.Boss.IF_I_HITTED_GO_AWAY_SEC_HIT) && !BotOwner.Memory.IsUnderFire)
                        {
                            return new Action(typeof(HoldPositionLogic), "Mcs:deltaLastHi");
                        }
                        else
                        {
                            return new Action(typeof(AttackMovingLogic), "Mcs:deltaLastHi");
                        }
                    }
                    else
                    {
                        if (isProtectCareKill)
                        {
                            if (!CanShootNow() && Time.time - goalEnemy.PersonalSeenTime < 3f)
                            {
                                return new Action(typeof(HoldPositionLogic), "Mcs:goalEnemy.P");
                            }
                            if (isProtectWantKill)
                            {
                                return new Action(typeof(GoToEnemyLogic), "Mcs:wantKill");
                            }
                        }
                        if (BotOwner.Memory.IsInCover)
                        {
                            if (BotOwner.Medecine.FirstAid.Have2Do && (BotOwner.Memory.LastEnemy == null || Time.time - BotOwner.Memory.LastEnemyTimeSeen > BotOwner.Settings.FileSettings.Mind.PROTECT_DELTA_HEAL_SEC))
                            {
                                return new Action(typeof(HealLogic), "Mcs:PROTECTDELT");
                            }
                        }
                        else if (_haveCoverToShoot)
                        {
                            if (BotOwner.Memory.IsInCover)
                            {
                                return new Action(typeof(HoldPositionLogic), "Mcs:HaveCoverSh1");
                            }
                            return new Action(typeof(GoToCoverPointLogic), "Mcs:HaveCoverSh2");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:Exception");
            }
        }

        public override bool IsActive()
        {
            if (!IsMcsBotPlayer)
            {
                return false;
            }

            if (BotOwner.Memory.HaveEnemy)
            {
                return true;
            }

            return false;
        }
    }
}