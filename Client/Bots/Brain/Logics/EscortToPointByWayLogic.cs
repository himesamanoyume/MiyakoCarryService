using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class EscortToPointByWayLogic : McsBotBaseLogic
    {
        private Vector3 _lastLeadPos = Vector3.zero;
        private Vector3 _lastPathTargetPos = Vector3.zero;
        private Vector3[] _lastCalcCorners = null; 
        private bool _lastCanRunResult = false;
        private float _lastPathUpdateTime = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.3f;
        private const float LEAD_POS_CHANGE_THRESHOLD_SQR = 1f;

        public EscortToPointByWayLogic(BotOwner botOwner) : base(botOwner)
        {

        }

        public override void Start()
        {
            base.Start();
            BotOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.FollowMe
            });
        }

        public override void Update(CustomLayer.ActionData data)
        {
            BotOwner.SetPose(1f);
            BotOwner.SetTargetMoveSpeed(1f);

            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }

            if (_lastPathUpdateTime < Time.time)
            {
                _lastPathUpdateTime = Time.time + PATH_UPDATE_INTERVAL;
                BotOwner.Steering.LookToMovingDirection();
                if (!mcsBotPlayerData.EscortPos.HasValue)
                {
                    return;
                }

                var sqrDistance = BotOwner.Position.McsSqrDistance(mcsBotPlayerData.LeadPlayer.Position);
                BotOwner.SetTargetMoveSpeed(1f);

                if (sqrDistance >= 5f * 5f && sqrDistance < 15f * 15f)
                {
                    BotOwner.Sprint(false, true);
                }
                else
                {
                    BotOwner.Sprint(true, true);
                }

                if (CanGetPathToRun(mcsBotPlayerData.LeadPlayer.Position, mcsBotPlayerData.EscortPos.Value, mcsBotPlayerData, out Vector3[] corners))
                {
                    var targetPoint = GetPointAlongPathAtDistance(corners, 10f);
                    BotOwner.GoToPoint(targetPoint, mustHaveWay: true);
                }
            }
        }

        private bool CanGetPathToRun(Vector3 leadPos, Vector3 targetPos, McsBotPlayerData mcsBotPlayerData, out Vector3[] corners)
        {
            var targetUnchanged = _lastPathTargetPos.McsSqrDistance(targetPos) < 0.09f;
            var leadUnchanged = _lastLeadPos.McsSqrDistance(leadPos) < LEAD_POS_CHANGE_THRESHOLD_SQR;

            if (targetUnchanged && leadUnchanged && _lastCalcCorners != null)
            {
                corners = _lastCalcCorners;
                return _lastCanRunResult;
            }

            var navMeshPath = new NavMeshPath();
            NavMesh.CalculatePath(leadPos, targetPos, -1, navMeshPath);
            var flag = false;

            if (navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                flag = true;
                if ((targetPos - navMeshPath.corners[navMeshPath.corners.Length - 1]).magnitude > 2f)
                {
                    flag = false;
                }
            }

            if (!flag && Tools.BetterDestination(1f, targetPos, out var betterDest))
            {
                navMeshPath = new NavMeshPath();
                NavMesh.CalculatePath(leadPos, betterDest, -1, navMeshPath);
                if (navMeshPath.status is NavMeshPathStatus.PathComplete or NavMeshPathStatus.PathPartial)
                {
                    flag = true;
                }
            }

            if (!flag)
            {
                corners = null;
                _lastCanRunResult = false;
                mcsBotPlayerData.EscortPos = null;
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Negative,
                });
                return _lastCanRunResult;
            }

            _lastPathTargetPos = targetPos;
            _lastLeadPos = leadPos;
            _lastCalcCorners = navMeshPath.corners;
            corners = _lastCalcCorners;
            _lastCanRunResult = true;
            return _lastCanRunResult;
        }

        private Vector3 GetPointAlongPathAtDistance(Vector3[] corners, float distance)
        {
            var accumulated = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                var segLen = Vector3.Distance(corners[i], corners[i + 1]);
                if (accumulated + segLen >= distance)
                {
                    var t = (distance - accumulated) / segLen;
                    return Vector3.Lerp(corners[i], corners[i + 1], t);
                }
                accumulated += segLen;
            }
            return corners[corners.Length - 1];
        }
    }
}