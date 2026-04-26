
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class FollowerPatrolLogic : McsBotBaseLogic
    {
        private FollowerPatrolBaseLogic _baseLogic;

        public FollowerPatrolLogic(BotOwner botOwner) : base(botOwner)
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
    }
}