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
                McsBotPlayerData.IsLooting = false;
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
                    return new Action(typeof(SimplePatrolLogic), "Mcs:leadPosNull");
                }

                if (McsBotPlayerData.ProxyPos.HasValue)
                {
                    return new Action(typeof(EscortToPointByWayLogic), "Mcs:EscortToPoint");
                }
                else if (!string.IsNullOrEmpty(McsBotPlayerData.ProxyTargetId))
                {
                    return new Action(typeof(EscortToPointByWayLogic), "Mcs:EscortToPoint");
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

            if (McsBotPlayerData.HasDecision([EDecision.ShouldQuestProxyAction, EDecision.ShouldLootProxyAction, EDecision.ShouldInteractionProxyAction]) && (McsBotPlayerData.ProxyPos.HasValue || !string.IsNullOrEmpty(McsBotPlayerData.ProxyTargetId)))
            {
                return true;
            }

            return false;
        }
    }
}