using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class RunToPointOverrideLogic : GoToPointBaseLogic
    {
        public RunToPointOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(MoveIntent data)
        {
            var canRun = method_0(true) == DoorInteractionStatus.CanRun;
            BotOwner_0.SetTargetMoveSpeed(1f);
            BotOwner_0.SetPose(1f);
            if (canRun && BotOwner_0.Mover.HasPathAndNoComplete)
            {
                BotOwner_0.Steering.LookToMovingDirection();
            }
            else
            {
                BotOwner_0.LookData.SetLookPointByHearing(null);
            }
            BotOwner_0.Sprint(true, true);
            if (!BotOwner_0.Mover.IsComeTo(BotOwner_0.Settings.FileSettings.Move.REACH_DIST, false, null))
            {
                return;
            }
            BotOwner_0.StopMove();
        }
    }
}