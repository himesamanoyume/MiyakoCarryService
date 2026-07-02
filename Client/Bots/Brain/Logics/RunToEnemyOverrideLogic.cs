using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class RunToEnemyOverrideLogic : RunToEnemyBaseLogic
    {
        public RunToEnemyOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(BaseIntent data)
        {
            var canRun = method_0() == DoorInteractionStatus.CanRun;
            BotOwner_0.SetTargetMoveSpeed(1f);
            NotMovingCheck();
            BotOwner_0.SetPose(1f);
            if (canRun && BotOwner_0.Mover.HasPathAndNoComplete)
            {
                BotOwner_0.Steering.LookToMovingDirection();
            }
            else
            {
                BotOwner_0.LookData.SetLookPointByHearing(null);
            }
            BotOwner_0.Sprint(true, false);
            if (BotOwner_0.Mover.IsComeTo(BotOwner_0.Settings.FileSettings.Move.REACH_DIST, false, null))
            {
                BotOwner_0.StopMove();
            }
        }
    }
}