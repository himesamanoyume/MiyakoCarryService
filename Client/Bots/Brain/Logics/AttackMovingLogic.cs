
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    internal sealed class AttackMovingLogic : McsBotBaseLogic
    {
        private AttackMovingBaseLogic _baseLogic;

        public AttackMovingLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}