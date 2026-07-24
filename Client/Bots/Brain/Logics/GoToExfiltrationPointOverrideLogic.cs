
using EFT;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class GoToExfiltrationPointOverrideLogic : GoToExfiltrationPointNodeBaseLogic
    {
        public GoToExfiltrationPointOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(BaseIntent data)
        {
            DoorOpen(true);
            var exfiltrationData = _owner.PatrollingData.ExfiltrationData;
            var cachedExfiltrationPoint = exfiltrationData.CachedExfiltrationPoint;
            var sqrDistance = _owner.Position.McsSqrDistance(cachedExfiltrationPoint.GetPosition(_owner));
            if (sqrDistance <= 9f)
            {
                _owner.StopMove();
                _owner.Steering.LookToPoint(_pointToLook);
                exfiltrationData.ComeToExfiltrationPoint();
            }
            else
            {
                _owner.Sprint(true, false);
                _gotoPeriod.Update();
                _pointToLook = _owner.Position + BotOwner.STAY_HEIGHT;
            }
            if (sqrDistance <= 9f)
            {
                if (_owner.Exfiltration.LeaveTime > Time.time + 99999f)
                {
                    _owner.Exfiltration.SetLeaveTime(Time.time + cachedExfiltrationPoint.ExfiltrationTime);
                }
                if (Time.time > _owner.Exfiltration.LeaveTime)
                {
                    _owner.LeaveData.RemoveFromMap();
                    return;
                }
            }
            else
            {
                _owner.Exfiltration.ResetLeaveTime();
            }
        }
    }
}