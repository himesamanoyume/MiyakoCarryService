using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class RunToPointOverrideLogic : GoToPointBaseLogic
    {
        public RunToPointOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(MoveIntent data)
        {
            botOwner_0.SetPose(1f);
            botOwner_0.SetTargetMoveSpeed(1f);
            botOwner_0.Sprint(true, false);
            botOwner_0.GoToSomePointData.UpdateToGo(botOwner_0.Settings.FileSettings.Move.CAN_SPRINT_GO_TO_SOME_POINT);
        }
    }
}