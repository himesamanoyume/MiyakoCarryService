
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

        public override Action GetNextAction()
        {
            try
            {
                if (ShouldShootImmediately())
                {
                    return new Action(typeof(ShootFromStationaryLogic), "Mcs:ShootImmediately");
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

                var canShoot = goalEnemy.CanShoot;
                var isProtectWantKill = ProtectWantKill();
                var isProtectCareKill = ProtectCareKill();

                UpdateCoverToShoot();

                if (!goalEnemy.IsVisible && BotOwner.SmokeGrenade.ShallShoot() && (BotOwner.Position - goalEnemy.Person.Position).sqrMagnitude <= 30f)
                {
                    return new Action(typeof(ShootToSmokeLogic), "Mcs:SmokeGrenad");
                }
                else if (_haveCoverToShoot && isProtectWantKill)
                {
                    var cannotSeeEnemy = CannotSeeEnemy(goalEnemy);
                    var canShootNow = CanShootNow();
                    if (cannotSeeEnemy && !canShootNow && Time.time - goalEnemy.PersonalSeenTime < 3f)
                    {
                        return new Action(typeof(RunToEnemyLogic), "Mcs:findEnemy1");
                    }
                    else if (!canShootNow && goalEnemy.Distance > 10f)
                    {
                        return new Action(typeof(AttackMovingLogic), "Mcs:goal.D");
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
                        var closestFriend = BotOwner.Covers.GetClosestFriend(out var sqrDist);
                        safeFire = sqrDist >= 3f || closestFriend == null || closestFriend.Id > BotOwner.Id;
                    }

                    if (safeFire)
                    {
                        if (goalEnemy.IsVisible)
                        {
                            return new Action(typeof(ShootFromPlaceLogic), "Mcs:goalEnemy.V");
                        }
                        else
                        {
                            return new Action(typeof(RunToEnemyLogic), "Mcs:deltaLastHi");
                        }
                    }
                    else
                    {
                        if (isProtectCareKill)
                        {
                            if (!CanShootNow() && Time.time - goalEnemy.PersonalSeenTime < 3f)
                            {
                                return new Action(typeof(RunToEnemyLogic), "Mcs:findEnemy2");
                            }
                        }

                        return new Action(typeof(RunToEnemyLogic), "Mcs:findEnemy3");
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