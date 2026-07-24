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
            _owner.SetPose(1f);
            _owner.SetTargetMoveSpeed(1f);
            _owner.Sprint(true, false);
            _owner.Steering.LookToMovingDirection();
            DoorOpen();
            if (data != null && !data.Used)
            {
                data.Used = true;
                _owner.GoToSomePointData.SetPoint(data.Point);
            }
            _owner.GoToSomePointData.UpdateToGo(_owner.Settings.FileSettings.Move.CAN_SPRINT_GO_TO_SOME_POINT);
        }
    }
}