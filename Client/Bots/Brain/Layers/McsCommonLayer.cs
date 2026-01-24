
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    internal class McsCommonLayer : McsBaseLayer<McsCommonLayer>
    {
        private float _nextReloadTime;
        public McsCommonLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override Action GetNextAction()
        {
            // MiyakoCarryServicePlugin.Logger.LogInfo($"Bot {BotOwner.name} calling GetNextAction");
            try
            {
                if (BotOwner.Medecine.FirstAid.Have2Do || BotOwner.Medecine.SurgicalKit.HaveWork)
                {
                    // If we are already in cover, we can heal
                    if (BotOwner.Memory.IsInCover)
                    {
                        return new Action(typeof(HealLogic), "Mcs:first aid");
                    }

                    // If we were hit in the last 20 seconds, run to cover before healing
                    if (WasHitRecently(20f))
                    {
                        return new Action(typeof(RunToCoverLogic), "Mcs:goforheal");
                    }

                    // Otherwise we can heal
                    return new Action(typeof(HealLogic), "Mcs:heal now");
                }

                // If we're in a smoke grenade, go to a cover point
                if (BotOwner.SmokeGrenade.IsInSmoke)
                {
                    return new Action(typeof(GoToCoverPointLogic), "Mcs:PeaceSmoke");
                }

                // 
                if (BotOwner.PeaceHardAim.HaveActions())
                {
                    return new Action(typeof(PeaceHardAimLogic), "Mcs:PeaceHardAi");
                }

                // 
                if (BotOwner.PeaceLook.HaveActions())
                {
                    return new Action(typeof(PeaceLookLogic), "Mcs:PeaceLook");
                }

                // 
                if (BotOwner.SecondWeaponData.HaveActions())
                {
                    return new Action(typeof(WatchSecondWeaponLogic), "Mcs:Look2ndWeap");
                }

                // Do some specific things if we aren't a boss or follower
                // if (!IsBossOrFollower())
                // {
                //     // Do the wiggle
                //     if (BotOwner.FriendlyTilt.HaveActions())
                //     {
                //         return new Action(typeof(FriendlyTiltLogic), "Mcs:FriendlyTil");
                //     }

                //     // Should we be eating/drinking?
                //     if (BotOwner.EatDrinkData.HaveActions())
                //     {
                //         return new Action(typeof(EatDrinkLogic), "Mcs:EatDrinkDat");
                //     }

                //     // Did a player gesture to us?
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

                // Get patrolling information
                BotOwner.PatrollingData.SetTargetMoveSpeed();
                BotOwner.PatrollingData.PointChooser.ShallChangeWay(false);
                var patrolWay = GetCurrentPatrolWay();

                // If we are not a boss, and we're following a boss, set our action to FollowerPatrol
                if (!BotOwner.Boss.IamBoss && BotOwner.BotFollower.HaveBoss)
                {
                    return new Action(typeof(FollowerPatrolLogic), "Mcs:BossFollow");
                }

                // Reload if we're under 60% ammo, and it's been long enough since our last reload
                float ammoPercent = BotOwner.WeaponManager.Reload.BulletCount / BotOwner.WeaponManager.Reload.MaxBulletCount;
                if (ammoPercent < 0.6f && Time.time >= _nextReloadTime)
                {
                    _nextReloadTime = Time.time + 30f;
                    BotOwner.WeaponManager.Reload.TryReload();
                }

                // If we have a patrol, it's a reserve patrol, and the bot is allowed on reserve patrols, set the action to alternative patrol
                if (patrolWay != null && patrolWay.PatrolType == PatrolType.reserved && BotOwner.Settings.FileSettings.Patrol.CAN_CHOOSE_RESERV)
                {
                    BotOwner.PatrollingData.ComeToPoint();
                    return new Action(typeof(AlternativePatrolLogic), "Mcs:RESER");
                }

                return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                // return new Action(typeof(McsBotPlayerPatrolLogic), "nothing to do");
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
            }
        }

        public override bool IsActive()
        {
            BotOwner.PriorityAxeTarget.FindTarget();
            // if (BotOwner.BotFollower.HaveBoss && IsMcsBotPlayer)
            if (IsMcsBotPlayer)
            {
                return true;
                // var mcsBossPlayer = McsBotPlayerData.BossPlayer;
                // var distance = Vector3.Distance(BotOwner.Position, mcsBossPlayer.Position);
                // MiyakoCarryServicePlugin.Logger.LogInfo($"Bot {BotOwner.name} distance {distance}");
                // if (distance >= 25)
                // {
                //     return true;
                // }
            }
            // if (BotOwner.Memory.HaveEnemy || BotOwner.Memory.IsUnderFire)
            // {
            //     return false;
            // }

            // if (BotOwner.BotFollower.HaveBoss && IsMcsBotPlayer)
            // {
            //     var mcsBossPlayer = McsBotPlayerData.BossPlayer;
            //     if (Vector3.Distance(BotOwner.Position, mcsBossPlayer.Position) >= 25)
            //     {
            //         MiyakoCarryServicePlugin.Logger.LogInfo($"Bot {BotOwner.name} calling IsActive return true!");
            //         return true;
            //     }
            // }
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            // MiyakoCarryServicePlugin.Logger.LogInfo($"Bot {BotOwner.name} calling IsCurrentActionEnding");
            // var mcsBossPlayer = McsBotPlayerData.BossPlayer;
            // if (Vector3.Distance(BotOwner.Position, mcsBossPlayer.Position) >= 25)
            // {
            //     return false;
            // }
            // // MiyakoCarryServicePlugin.Logger.LogInfo($"Bot {BotOwner.name} calling IsCurrentActionEnding");
            // return true;

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

            // // If we have a boss, return its patrolling data PatrolWay
            // if (BotOwner.BotFollower.HaveBoss)
            // {
            //     return BotOwner.BotFollower.BossToFollow.PatrollingData.Way;
            // }

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