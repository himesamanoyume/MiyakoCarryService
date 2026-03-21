
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;
using UnityEngine.AI;

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

                if (!goalEnemy.IsVisible && BotOwner.SmokeGrenade.ShallShoot() && BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position) <= 40f * 40f)
                {
                    return new Action(typeof(ShootToSmokeLogic), "Mcs:SmokeGrenad");
                }
                else
                {
                    var safeFire = false;
                    if (canShoot)
                    {
                        var closestFriend = BotOwner.Covers.GetClosestFriend(out var sqrDist);
                        safeFire = sqrDist >= 1f || closestFriend == null || closestFriend.Id > BotOwner.Id;
                    }

                    if (safeFire)
                    {
                        if (goalEnemy.IsVisible)
                        {
                            return new Action(typeof(ShootFromPlaceLogic), "Mcs:ShootFromPlace");
                        }
                        else
                        {
                            return new Action(typeof(RunToEnemyLogic), "Mcs:DeltaLastHi");
                        }
                    }
                    else
                    {
                        // if (isProtectCareKill)
                        // {
                        //     if (CanShootNow() && Time.time - goalEnemy.PersonalSeenTime < 3f)
                        //     {
                        //         return new Action(typeof(RunToEnemyLogic), "Mcs:findEnemy2");
                        //     }
                        // }

                        var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
                        if (mcsLeadPlayerPos == null)
                        {
                            return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:leadPosNull");
                        }

                        if (mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) <= 50f * 50f)
                        {
                            return new Action(typeof(RunToEnemyLogic), "Mcs:RushEnemy");
                        }
                        else
                        {
                            
                            Vector3? validPosition = null;
                            var xOffset = GClass856.Random(1f, 3f) * GClass856.RandomSing();
                            var zOffset = GClass856.Random(1f, 3f) * GClass856.RandomSing();
                            var newPos = mcsLeadPlayerPos + new Vector3(xOffset, 0f, zOffset);

                            for (int attempt = 0; attempt < 30; attempt++)
                            {
                                if (NavMesh.SamplePosition(newPos, out var navMeshHit1, 7f, -1))
                                {
                                    if (Mathf.Abs(navMeshHit1.position.y - mcsLeadPlayerPos.y) <= 2f)
                                    {
                                        validPosition = navMeshHit1.position;
                                        break;
                                    }
                                }
                            }

                            if (validPosition == null && NavMesh.SamplePosition(newPos, out var navMeshHit2, 7f, -1))
                            {
                                validPosition = navMeshHit2.position;
                            }

                            if (BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= _closeLeadDistance)
                            {
                                if (validPosition.HasValue)
                                {
                                    BotOwner.GoToSomePointData.SetPoint(validPosition.Value);
                                    return new Action(typeof(GoToPointLogic), "Mcs:GoToPointLogic");
                                }

                                return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:CannotFindPath1");
                            }
                            else
                            {
                                if (_lastPatrolTime + 8f < Time.time)
                                {
                                    _lastPatrolTime = Time.time + 8f;
                                    if (validPosition.HasValue)
                                    {
                                        BotOwner.GoToSomePointData.SetPoint(validPosition.Value);
                                        return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                                    }

                                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:CannotFindPath2");
                                }
                                else
                                {
                                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPosition");
                                }
                            }
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