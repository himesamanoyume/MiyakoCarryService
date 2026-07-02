
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class HoldPositionOverrideLogic : HoldPositionBaseLogic
    {
        public HoldPositionOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(TimedFireIntent data)
        {
            BotOwner_0.Sprint(false, false);
            BotOwner_0.StopMove();
            if (data != null && data.FinishTime < Time.time)
            {
                BotOwner_0.Memory.Spotted(false, null, null);
                return;
            }
            method_13();
            if (BotOwner_0.Memory.GoalEnemy != null && BotOwner_0.Memory.GoalEnemy.IsVisible)
            {
                if (BotOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER && BotOwner_0.BotLay.IsLay)
                {
                    if (BotOwner_0.Memory.GoalEnemy.Distance > BotOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY)
                    {
                        BotOwner_0.Steering.LookToPoint(BotOwner_0.Memory.GoalEnemy.CurrPosition);
                    }
                }
                else
                {
                    BotOwner_0.Steering.LookToPoint(BotOwner_0.Memory.GoalEnemy.CurrPosition);
                }
            }
            else if ((BotOwner_0.Memory.GoalEnemy == null || BotOwner_0.Memory.GoalEnemy.IsVisible || !BotOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER || !BotOwner_0.BotLay.IsLay || BotOwner_0.Memory.GoalEnemy.Distance >= BotOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY) && BotOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY < 0f)
            {
                Look();
            }
            if (BotOwner_0.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Ambush) && CustomNavigationPoint_0 != null)
            {
                BotOwner_0.BotLight.TurnOff(true, false);
            }
            if (BotOwner_0.Memory.IsInCover)
            {
                if (BotOwner_0.Settings.FileSettings.Cover.CAN_LAY_TO_COVER)
                {
                    if (!BotOwner_0.BotLay.TryLay())
                    {
                        BotOwner_0.SetPose(0.1f);
                        return;
                    }
                }
                else if (BotOwner_0.Settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING)
                {
                    BotOwner_0.SetPose(0.1f);
                    return;
                }
            }
            else if (BotOwner_0.Settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING)
            {
                BotOwner_0.SetPose(0.1f);
            }
        }
    }
}