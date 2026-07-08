
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class HoldPositionOverrideLogic : HoldPositionBaseLogic
    {
        public HoldPositionOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(TimedFireIntent data)
        {
            botOwner_0.Sprint(false, false);
            botOwner_0.StopMove();
            if (data != null && data.FinishTime < Time.time)
            {
                botOwner_0.Memory.Spotted(false, null, null);
                return;
            }
            CheckWantReload();
            if (botOwner_0.Memory.GoalEnemy != null && botOwner_0.Memory.GoalEnemy.IsVisible)
            {
                if (botOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER && botOwner_0.BotLay.IsLay)
                {
                    if (botOwner_0.Memory.GoalEnemy.Distance > botOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY)
                    {
                        botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.CurrPosition);
                    }
                }
                else
                {
                    botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.CurrPosition);
                }
            }
            else if ((botOwner_0.Memory.GoalEnemy == null || botOwner_0.Memory.GoalEnemy.IsVisible || !botOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER || !botOwner_0.BotLay.IsLay || botOwner_0.Memory.GoalEnemy.Distance >= botOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY) && botOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY < 0f)
            {
                Look();
            }
            if (botOwner_0.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Ambush) && CovPoint != null)
            {
                botOwner_0.BotLight.TurnOff(true, false);
            }
            if (botOwner_0.Memory.IsInCover)
            {
                if (botOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER)
                {
                    if (!botOwner_0.BotLay.TryLay())
                    {
                        botOwner_0.SetPose(0.1f);
                        return;
                    }
                }
                else if (botOwner_0.Settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING)
                {
                    botOwner_0.SetPose(0.1f);
                    return;
                }
            }
            else if (botOwner_0.Settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING)
            {
                botOwner_0.SetPose(0.1f);
            }
        }
    }
}