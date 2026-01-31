
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    internal sealed class GoToPointLogic : McsBotBaseLogic
    {
        private GoToPointBaseLogic _baseLogic;

        public GoToPointLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            if (Random.Range(0, 100) > 90)
            {
                BotOwner.ShowSubtitleMsg(string.Format("<b>{0}</b>:正在前往目的地".McsLocalized(), BotOwner.Profile.Nickname));
            }
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}