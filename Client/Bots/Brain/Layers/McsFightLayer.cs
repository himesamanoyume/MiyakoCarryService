
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsFightLayer : McsBaseLayer<McsFightLayer>
    {
        public McsFightLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            InitActionMap();
        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.ShouldHoldPosition = false;
                McsBotPlayerData.ShouldGoToPoint = false;
                McsBotPlayerData.IsLooting = false;
            }

            BotOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.OnFirstContact,
                Position = BotOwner.Memory.GoalEnemy.EnemyLastPosition
            });
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override Action GetNextAction()
        {
            try
            {
                var goalEnemy = BotOwner.Memory.GoalEnemy;
                if (goalEnemy == null)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:!HaveEnemy");
                }

                var haveBullets = BotOwner?.WeaponManager?.HaveBullets;

                if (haveBullets.Value && IsShootFromCoverConditionAllFine())
                {
                    return new Action(typeof(ShootFromCoverLogic), "Mcs:ShootFromCover");
                }

                if (BotOwner.NearDoorData.RecentlyClosedDoorCheckTime + 0.3f < Time.time && BotOwner.BotsGroup.EnemyLastSeenTimeReal + 7f >= Time.time && GetCrossPoint(goalEnemy))
                {
                    BotOwner.Memory.Spotted(false, null, null);
                }

                if (ShouldUseMeleeAttack())
                {
                    return new Action(typeof(MeleeAttackLogic), "Mcs:MeleeAttack");
                }

                if (!haveBullets.Value)
                {
                    BotOwner.WeaponManager.Reload.McsTryReload();
                }

                if (BotOwner.WeaponManager.UnderbarrelLauncherController.NeedToReload())
                {
                    BotOwner.WeaponManager.UnderbarrelLauncherController.TryReload();
                }

                var canShoot = goalEnemy.CanShoot;
#if DEBUG
                // if (canShoot)
                // {
                //     MiyakoCarryServicePlugin.Logger.LogWarning($"CanShoot: {canShoot}, Distance: {goalEnemy.Distance}, IsVisible: {goalEnemy.IsVisible}");
                // }
#endif
                var isProtectWantKill = ProtectWantKill();
                var isProtectCareKill = ProtectCareKill();

                UpdateCoverToShoot();

                if (!goalEnemy.IsVisible && BotOwner.SmokeGrenade.ShallShoot() && BotOwner.Position.McsSqrDistance(goalEnemy.Person.Position) <= 40f * 40f)
                {
                    return new Action(typeof(ShootToSmokeLogic), "Mcs:SmokeGrenad");
                }
                else
                {
                    var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
                    if (mcsLeadPlayerPos == null)
                    {
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:leadPosNull");
                    }

                    if (_nextUpdatePosTime < Time.time)
                    {
                        UpdateMoveTarget(out float nextTime);
                        _nextUpdatePosTime = Time.time + nextTime;
                    }

                    var safeFire = false;
                    if (canShoot)
                    {
                        var closestFriend = BotOwner.Covers.GetClosestFriend(out var sqrDist);
                        safeFire = sqrDist >= 1f || closestFriend == null || closestFriend.Id > BotOwner.Id;
                    }

                    if (safeFire && haveBullets.Value)
                    {
                        if (goalEnemy.IsVisible)
                        {
                            if (!BotOwner.GoToSomePointData.IsCome())
                            {
                                return new Action(typeof(AttackMovingLogic), "Mcs:AttackMoving");
                            }
                            else
                            {
                                return new Action(typeof(ShootFromPlaceLogic), "Mcs:ShootFromPlace");
                            }
                        }
                        else
                        {
                            return new Action(typeof(RunToEnemyLogic), "Mcs:SafeButNotVisible");
                        }
                    }
                    else
                    {
                        if (mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position) <= 50f * 50f)
                        {
                            return new Action(typeof(RunToEnemyLogic), "Mcs:RushEnemy");
                        }
                        else
                        {
                            if (BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= TOO_FAR_FROM_LEAD_DISTANCE)
                            {
                                if (_currentMoveTarget.HasValue)
                                {
                                    BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                                    return new Action(typeof(GoToPointLogic), "Mcs:GoToPointLogic");
                                }

                                return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:CannotFindPath1");
                            }
                            else
                            {
                                if (_nextPatrolTime + 4f < Time.time)
                                {
                                    _nextPatrolTime = Time.time + 4f;

                                    var validPosition = GetPosNearMcsLeadPlayer(mcsLeadPlayerPos);
                                    if (validPosition.HasValue)
                                    {
                                        BotOwner.GoToSomePointData.SetPoint(validPosition.Value);
                                        return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                                    }

                                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:CannotFindPath2");
                                }
                                else
                                {
                                    if (BotOwner.Memory.GoalEnemy != null && Time.time - BotOwner.Mover.LastTimePosChanged > 8f)
                                    {
                                        // 借鉴SAIN
                                        var angle = UnityEngine.Random.Range(70, 110);
                                        if (GClass856.RandomBool())
                                        {
                                            angle *= -1;
                                        }

                                        var directionToEnemy = BotOwner.Memory.GoalEnemy.Person.LookDirection.normalized;
                                        var rotated = Quaternion.Euler(0, angle, 0) * directionToEnemy;
                                        rotated.y = 0;
                                        rotated *= 7f;
                                        rotated += UnityEngine.Random.insideUnitSphere;
                                        if (Tools.BetterDestination(3f, mcsLeadPlayerPos + rotated, out var targetPos))
                                        {
                                            BotOwner.GoToSomePointData.SetPoint(targetPos);
                                            return new Action(typeof(GoToProtectLogic), "Mcs:Protect");
                                        }
                                    }

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
                // 使护航下的Zyriachy无视目标处于灯塔限定区域时才可视为敌人的限制
                if (BotOwner.Profile.Info.Settings.Role is WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
                {
                    var goalEnemy = BotOwner.Memory.GoalEnemy;
                    if (goalEnemy != null && goalEnemy.Person != null)
                    {
                        if (BotOwner.Boss.BossLogic is ZyriachyBossLogicClass zyriachyBossLogicClass)
                        {
                            zyriachyBossLogicClass.AddEnemy(goalEnemy.Person, EBotEnemyCause.zryachiyLogic);
                        }
                    }
                }
                return true;
            }

            return false;
        }
    }
}