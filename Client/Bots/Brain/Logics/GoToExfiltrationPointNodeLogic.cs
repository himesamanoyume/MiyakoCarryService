
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class GoToExfiltrationPointNodeLogic : McsBotBaseLogic
    {
        private GoToExfiltrationPointOverrideLogic _baseLogic;

        public GoToExfiltrationPointNodeLogic(BotOwner botOwner) : base(botOwner)
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

    public class GoToExfiltrationPointOverrideLogic : GoToExfiltrationPointNodeBaseLogic
    {
        public GoToExfiltrationPointOverrideLogic(BotOwner bot) : base(bot)
        {

        }

        public override void UpdateNodeByBrain(BaseIntent data)
        {
            DoorOpen(true);
            var exfiltrationData = botOwner_0.PatrollingData.ExfiltrationData;
            var cachedExfiltrationPoint = exfiltrationData.CachedExfiltrationPoint;
            var sqrDistance = botOwner_0.Position.McsSqrDistance(cachedExfiltrationPoint.GetPosition(botOwner_0));
            if (sqrDistance <= 9f)
            {
                botOwner_0.StopMove();
                botOwner_0.Steering.LookToPoint(vector3_0);
                exfiltrationData.ComeToExfiltrationPoint();
            }
            else
            {
                botOwner_0.Sprint(true, false);
                aIPeriodAction.Update();
                vector3_0 = botOwner_0.Position + BotOwner.STAY_HEIGHT;
            }
            if (sqrDistance <= 9f)
            {
                if (botOwner_0.Exfiltration.LeaveTime > Time.time + 99999f)
                {
                    botOwner_0.Exfiltration.SetLeaveTime(Time.time + cachedExfiltrationPoint.ExfiltrationTime);
                }
                if (Time.time > botOwner_0.Exfiltration.LeaveTime)
                {
                    botOwner_0.LeaveData.RemoveFromMap();
                    return;
                }
            }
            else
            {
                botOwner_0.Exfiltration.ResetLeaveTime();
            }
        }
    }
}