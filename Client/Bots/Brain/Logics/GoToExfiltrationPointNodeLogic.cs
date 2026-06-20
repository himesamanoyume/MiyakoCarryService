
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class GoToExfiltrationPointNodeLogic : McsBotBaseLogic
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
            method_0(true);
            var exfiltrationData = BotOwner_0.PatrollingData.ExfiltrationData;
            var cachedExfiltrationPoint = exfiltrationData.CachedExfiltrationPoint;
            var sqrDistance = BotOwner_0.Position.McsSqrDistance(cachedExfiltrationPoint.GetPosition(BotOwner_0));
            if (sqrDistance <= 9f)
            {
                BotOwner_0.StopMove();
                BotOwner_0.Steering.LookToPoint(Vector3_0);
                exfiltrationData.ComeToExfiltrationPoint();
            }
            else
            {
                BotOwner_0.Sprint(true, true);
                Gclass25_0.Update();
                Vector3_0 = BotOwner_0.Position + BotOwner.STAY_HEIGHT;
            }
            if (sqrDistance <= 9f)
            {
                if (BotOwner_0.Exfiltration.LeaveTime > Time.time + 99999f)
                {
                    BotOwner_0.Exfiltration.SetLeaveTime(Time.time + cachedExfiltrationPoint.ExfiltrationTime);
                }
                if (Time.time > BotOwner_0.Exfiltration.LeaveTime)
                {
                    BotOwner_0.LeaveData.RemoveFromMap();
                    return;
                }
            }
            else
            {
                BotOwner_0.Exfiltration.ResetLeaveTime();
            }
        }
    }
}