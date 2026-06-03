
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class GoToPointLogic : McsBotBaseLogic
    {
        private GoToPointBaseLogic _baseLogic;

        public GoToPointLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            base.Start();
            BotOwner.SetTargetMoveSpeed(1f);
            BotOwner.Sprint(true, false);
            BotOwner.SetPose(1f);
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            BotOwner.SetTargetMoveSpeed(1f);
            BotOwner.Sprint(true, false);
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}