
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class HoldPositionLogic : McsBotBaseLogic
    {
        private HoldPositionOverrideLogic _baseLogic;

        public HoldPositionLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            BotOwner.Mover._lastPos = BotOwner.Position;
            base.Start();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            BotOwner.Mover._lastTimePosChanged = Time.time;
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}