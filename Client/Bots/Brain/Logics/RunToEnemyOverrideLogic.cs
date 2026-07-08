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
            var canRun = DoorOpen() == DoorInteractionStatus.CanRun;
            botOwner_0.SetTargetMoveSpeed(1f);
            NotMovingCheck();
            botOwner_0.SetPose(1f);
            if (canRun && botOwner_0.Mover.HasPathAndNoComplete)
            {
                botOwner_0.Steering.LookToMovingDirection();
            }
            else
            {
                botOwner_0.LookData.SetLookPointByHearing(null);
            }
            botOwner_0.Sprint(true, false);
            if (botOwner_0.Mover.IsComeTo(botOwner_0.Settings.FileSettings.Move.REACH_DIST, false, null))
            {
                botOwner_0.StopMove();
            }
        }
    }
}