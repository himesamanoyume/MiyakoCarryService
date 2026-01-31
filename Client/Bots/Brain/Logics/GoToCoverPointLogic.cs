
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    internal sealed class GoToCoverPointLogic : McsBotBaseLogic
    {
        private GoToCoverPointBaseLogic _baseLogic;

        public GoToCoverPointLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            if (Random.Range(0, 100) > 90)
            {
                BotOwner.ShowSubtitleMsg(string.Format("<b>{0}</b>:移动至掩体中".McsLocalized(), BotOwner.Profile.Nickname));
            }
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}