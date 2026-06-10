
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class RunToEnemyLogic : McsBotBaseLogic
    {
        private RunToEnemyOverrideLogic _baseLogic;

        public RunToEnemyLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
        }

        public sealed class RunToEnemyOverrideLogic : RunToEnemyBaseLogic
        {
            public RunToEnemyOverrideLogic(BotOwner bot) : base(bot)
            {

            }

            public override void UpdateNodeByBrain(BaseIntent data)
            {
                var canRun = method_0(true) == DoorInteractionStatus.CanRun;
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
                BotOwner_0.Sprint(true, true);
                if (BotOwner_0.Mover.IsComeTo(BotOwner_0.Settings.FileSettings.Move.REACH_DIST, false, null))
                {
                    BotOwner_0.StopMove();
                }
            }
        }
    }
}