
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsEscortLayer : McsBaseLayer
    {
        public McsEscortLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            
        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.IsLooting = false;
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.FollowMe
                });
            }
        }

        public override Action GetNextAction()
        {
            try
            {
                var time = Time.time;
                if (McsBotPlayerData == null)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Uninitialized");
                }

                if (McsBotPlayerData.TargetPos.HasValue)
                {
                    if (_nextUpdatePosTime < time)
                    {
                        UpdateEscortMoveTarget(McsBotPlayerData.TargetPos, out float nextTime);
                        _nextUpdatePosTime = time + nextTime;
                    }

                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                        return new Action(typeof(EscortToPointByWayLogic), "Mcs:EscortToPoint");
                    }

                    return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindEscortNearPath");
                }
                else
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindEscortPos");
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(SimplePatrolLogic), "Mcs:Exception");
            }
        }

        public override bool IsActive()
        {
            if (!IsMcsBotPlayer)
            {
                return false;
            }

#if DEBUG
            if (!MiyakoCarryServicePlugin.EnableMcsLayer.Value)
            {
                return false;
            }
#endif

            if (McsBotPlayerData == null)
            {
                return false;
            }

            if (BotOwner.Memory.IsUnderFire)
            {
                return false;
            }

            if (!McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
            {
                return false;
            }

            if (McsBotPlayerData.HasDecision(Decisions.ShouldEscort) && McsBotPlayerData.TargetPos.HasValue)
            {
                return true;
            }

            return false;
        }
    }
}