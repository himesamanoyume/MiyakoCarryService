
using System;
using Comfort.Common;
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
                    return new Action(typeof(HoldPositionLogic), "Mcs:Uninitialized");
                }

                if (McsBotPlayerData.HasIntent(Intents.ShouldEscortToBtr))
                {
                    var btrController = Singleton<GameWorld>.Instance.BtrController;
                    var side = btrController.BtrView.GetBtrSide(1);
                    if (side == null)
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindBtrSide");
                    }

                    var doorPos = side.GoInPoints().Item1;
                    if (_nextUpdatePosTime < time)
                    {
                        McsBotPlayerData.TargetPos = doorPos;
                        UpdateEscortMoveTarget(McsBotPlayerData.TargetPos, out float nextTime);
                        _nextUpdatePosTime = time + nextTime;
                    }

                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                        return new Action(typeof(EscortToPointByWayLogic), "Mcs:EscortToBtr");
                    }

                    return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindEscortNearPath");
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

                    return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindEscortNearPath");
                }
                else
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:CannotFindEscortPos");
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(HoldPositionLogic), "Mcs:Exception");
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

            if (CanShootNow())
            {
                return false;
            }

            if (!McsBotPlayerData.LeadPlayer.HealthController.IsAlive)
            {
                return false;
            }

            if ((McsBotPlayerData.HasIntent(Intents.ShouldEscort) && McsBotPlayerData.TargetPos.HasValue) || McsBotPlayerData.HasIntent(Intents.ShouldEscortToBtr))
            {
                return true;
            }

            return false;
        }
    }
}