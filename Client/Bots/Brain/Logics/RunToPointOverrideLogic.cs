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
            BotOwner_0.SetPose(1f);
            BotOwner_0.SetTargetMoveSpeed(1f);
            BotOwner_0.Sprint(true, true);
            BotOwner_0.GoToSomePointData.UpdateToGo(BotOwner_0.Settings.FileSettings.Move.CAN_SPRINT_GO_TO_SOME_POINT);
        }
    }
}