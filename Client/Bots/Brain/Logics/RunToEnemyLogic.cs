
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    internal sealed class RunToEnemyLogic : McsBotBaseLogic
    {
        private RunToEnemyNewLogic _baseLogic;

        public RunToEnemyLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
        }

        internal sealed class RunToEnemyNewLogic : RunToEnemyBaseLogic
        {
            public RunToEnemyNewLogic(BotOwner bot) : base(bot)
            {

            }

            public override void UpdateNodeByBrain(BaseIntent data)
            {
                var canRun = method_0(true) == DoorInteractionStatus.CanRun;
                BotOwner_0.SetTargetMoveSpeed(1f);
                NotMovingCheck();
                method_6();
                BotOwner_0.SetPose(1f);
                if (canRun && BotOwner_0.Mover.HasPathAndNoComplete)
                {
                    BotOwner_0.Steering.LookToMovingDirection();
                    BotOwner_0.Sprint(true, true);
                }
                else
                {
                    BotOwner_0.LookData.SetLookPointByHearing(null);
                    BotOwner_0.Sprint(true, true);
                }
                if (!BotOwner_0.Mover.IsComeTo(BotOwner_0.Settings.FileSettings.Move.REACH_DIST, false, null))
                {
                    return;
                }
                method_7();
            }
        }
    }
}