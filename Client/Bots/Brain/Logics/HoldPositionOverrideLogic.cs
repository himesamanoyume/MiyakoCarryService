
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class HoldPositionOverrideLogic : HoldPosition
    {
        public HoldPositionOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(ShootHoldResultParams data)
        {
            _owner.Sprint(false, false);
            _owner.StopMove();
            if (data != null && data.FinishTime < Time.time)
            {
                _owner.Memory.Spotted(false, null, null);
                return;
            }
            CheckWantReload();
            if (_owner.Memory.GoalEnemy != null && _owner.Memory.GoalEnemy.IsVisible)
            {
                if (_owner.Settings.FileSettings.Cover.CAN_LAY_TO_COVER && _owner.BotLay.IsLay)
                {
                    if (_owner.Memory.GoalEnemy.Distance > _owner.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY)
                    {
                        _owner.Steering.LookToPoint(_owner.Memory.GoalEnemy.CurrPosition);
                    }
                }
                else
                {
                    _owner.Steering.LookToPoint(_owner.Memory.GoalEnemy.CurrPosition);
                }
            }
            else if ((_owner.Memory.GoalEnemy == null || _owner.Memory.GoalEnemy.IsVisible || !_owner.Settings.FileSettings.Cover.CAN_LAY_TO_COVER || !_owner.BotLay.IsLay || _owner.Memory.GoalEnemy.Distance >= _owner.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY) && _owner.Settings.FileSettings.Cover.CAN_LAY_TO_COVER_DIST_LOOK_TO_ENEMY < 0f)
            {
                Look();
            }
            if (_owner.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Ambush) && CovPoint != null)
            {
                _owner.BotLight.TurnOff(true, false);
            }
            if (_owner.Memory.IsInCover)
            {
                if (_owner.Settings.FileSettings.Cover.CAN_LAY_TO_COVER)
                {
                    if (!_owner.BotLay.TryLay())
                    {
                        _owner.SetPose(0.1f);
                        return;
                    }
                }
                else if (_owner.Settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING)
                {
                    _owner.SetPose(0.1f);
                    return;
                }
            }
            else if (_owner.Settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING)
            {
                _owner.SetPose(0.1f);
            }
        }
    }
}