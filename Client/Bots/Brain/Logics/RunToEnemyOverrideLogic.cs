using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class RunToEnemyOverrideLogic : RunToEnemy
    {
        public RunToEnemyOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(CoreActionResultParams data)
        {
            var canRun = DoorOpen() == DoorInteractionStatus.CanRun;
            _owner.SetTargetMoveSpeed(1f);
            NotMovingCheck();
            _owner.SetPose(1f);
            if (canRun && _owner.Mover.HasPathAndNoComplete)
            {
                _owner.Steering.LookToMovingDirection();
            }
            else
            {
                _owner.LookData.SetLookPointByHearing(null);
            }
            _owner.Sprint(true, false);
            if (_owner.Mover.IsComeTo(_owner.Settings.FileSettings.Move.REACH_DIST, false, null))
            {
                _owner.StopMove();
            }
        }
    }
}