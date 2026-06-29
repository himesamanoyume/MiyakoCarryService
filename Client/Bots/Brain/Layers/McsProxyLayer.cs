using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsProxyLayer : McsBaseLayer<McsProxyLayer>
    {
        public McsProxyLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Going
                });
            }
        }

        public override Action GetNextAction()
        {
            try
            {
                if (McsBotPlayerData == null)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:LeadPosNull");
                }

                if (McsBotPlayerData.HasDecision(EDecision.ShouldHoldPosition))
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionForProxyAction");
                }

                if (McsBotPlayerData.TargetPos.HasValue)
                {
                    if (_nextUpdatePosTime < Time.time)
                    {
                        UpdateCommonMoveTarget(McsBotPlayerData.TargetPos, out float nextTime);
                        _nextUpdatePosTime = Time.time + nextTime;
                    }

                    if (_currentMoveTarget.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(_currentMoveTarget.Value);
                        return new Action(typeof(GoToExcuteProxyActionLogic), "Mcs:GoToExcuteProxyAction");
                    }

                    return new Action(typeof(SimplePatrolLogic), "Mcs:CannotFindProxyPos");
                }
                else
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:NoProxyTargetPos");
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

            if (McsBotPlayerData.HasDecision(EDecision.ShouldQuestProxyAction) || McsBotPlayerData.HasDecision(EDecision.ShouldLootProxyAction) || McsBotPlayerData.HasDecision(EDecision.ShouldInteractionProxyAction))
            {
                return true;
            }

            return false;
        }
    }
}