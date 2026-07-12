using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsAvoidDangerLayer : McsBaseLayer
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

                return new Action(typeof(HoldPositionLogic), "Mcs:NoDanger");
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

            if (BotOwner.WeaponManager.Grenades.ThrowindNow)
            {
                return false;
            }

            if (BotOwner.BewarePlantedMine.CanDeactivate())
            {
                return true;
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

        public override bool EndGoToCoverPoint()
        {
            if (!BotOwner.ArtilleryDangerPlace.ShallRunAway() && !BotOwner.BewareGrenade.ShallRunAway())
            {
                if (BotOwner.Memory.IsInCover && BotOwner.SmokeGrenade.IsInSmoke && BotOwner.Memory.CurCustomCoverPoint != null)
                {
                    BotOwner.Memory.CurCustomCoverPoint.Spotted(10f);
                }
                return true;
            }

            return true;
        }

        public override bool EndHoldPosition()
        {
            if (BotOwner.BewareGrenade.ShallRunAway() && BotOwner.Memory.IsInCover && BotOwner.BewareBTR.ShallRunAway() && BotOwner.Memory.HaveEnemy && BotOwner.Memory.GoalEnemy.CanShoot)
            {
                return true;
            }
            if (BotOwner.BewareGrenade.ShallRunAway() && BotOwner.Memory.IsInCover && BotOwner.Memory.CurCustomCoverPoint.CoverLevel == CoverLevel.Stay && BotOwner.Memory.CurCustomCoverPoint.IsGoodForGrenade(BotOwner.BewareGrenade.GrenadeDangerPoint, BotOwner) && !BotOwner.BewareBTR.ShallRunAway())
            {
                return false;
            }
            if (BotOwner.ArtilleryDangerPlace.IsActive)
            {
                return true;
            }
            return true;
        }
    }
}