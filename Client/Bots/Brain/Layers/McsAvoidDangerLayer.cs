using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsAvoidDangerLayer : McsBaseLayer<McsAvoidDangerLayer>
    {
        public McsAvoidDangerLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public override void Start()
        {
            base.Start();
            if (McsBotPlayerData != null)
            {
                McsBotPlayerData.IsLooting = false;
            }
        }

        public override Action GetNextAction()
        {
            try
            {
                if (BotOwner.FlashGrenade.IsFlashed)
                {
                    return new Action(typeof(FlashedLogic), "Mcs:Flashed");
                }

                if (BotOwner.BotTurnAwayLight.IsActive)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:TurnAwayLight");
                }

                if (BotOwner.ArtilleryDangerPlace.ShallRunAway())
                {
                    return new Action(typeof(RunAwayArtilleryLogic), "Mcs:RunAwayArtillery");
                }

                if (BotOwner.BewareGrenade.ShallRunAway())
                {
                    return new Action(typeof(RunAwayGrenadeLogic), "Mcs:RunAwayGrenade");
                }

                if (BotOwner.BewareBTR.ShallRunAway())
                {
                    return new Action(typeof(RunAwayBTRLogic), "Mcs:RunAwayBTR");
                }

                if (!BotOwner.Memory.HaveEnemy && BotOwner.SmokeGrenade.IsInSmoke)
                {
                    return new Action(typeof(GoToCoverPointLogic), "Mcs:PeaceSmoke");
                }

                if (BotOwner.BewarePlantedMine.CanDeactivate())
                {
                    return new Action(typeof(DeactivateMineLogic), "Mcs:DeactivateMine");
                }

                return new Action(typeof(HoldPositionLogic), "Mcs:HoldPosition");
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

            if (McsBotPlayerData == null)
            {
                return false;
            }

            if (BotOwner.WeaponManager.Grenades.ThrowindNow)
            {
                return false;
            }

            if (BotOwner.FlashGrenade.IsFlashed)
            {
                return true;
            }

            if (BotOwner.BotTurnAwayLight.IsActive)
            {
                return true;
            }

            if (BotOwner.ArtilleryDangerPlace.ShallRunAway())
            {
                return true;
            }

            if (BotOwner.BewareGrenade.ShallRunAway())
            {
                return true;
            }

            if (BotOwner.BewareBTR.ShallRunAway())
            {
                return true;
            }

            if (!BotOwner.Memory.HaveEnemy && BotOwner.SmokeGrenade.IsInSmoke)
            {
                return true;
            }

            return false;
        }
    }
}