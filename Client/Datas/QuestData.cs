
using System;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Quests;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class QuestData : TriggerData, IProxyActor
    {
        private WeakReference<QuestDataClass> _questRef;
        public QuestDataClass Quest => _questRef.TryGetTarget(out var quest) ? quest : null;
        private WeakReference<Condition> _conditionRef;
        public Condition QuestCondition => _conditionRef != null ? _conditionRef.TryGetTarget(out var condition) ? condition : null : null;
        private WeakReference<Transform> _transformRef;
        private Transform _questTransform => _transformRef.TryGetTarget(out var transform) ? transform : null;

        public QuestData(QuestDataClass quest, Transform questTransform, Condition condition) : base()
        {
            _conditionRef = new WeakReference<Condition>(condition);
            _questRef = new WeakReference<QuestDataClass>(quest);
            _transformRef = new WeakReference<Transform>(questTransform);
            _colliders = questTransform.GetComponentsInChildren<Collider>().ToList();
        }

        public override string GetActionName() => QuestCondition.id.ToString().McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, _questTransform.position)));
        
        public override bool IsDisabled() => false;

        public override void Dispose()
        {
            base.Dispose();
            _questRef = null;
            _conditionRef = null;
            _transformRef = null;
        }

        public void ForceCompleteQuest()
        {
            if (QuestCondition is ConditionLeaveItemAtLocation conditionLeaveItemAtLocation)
            {
                // 等待 conditionLeaveItemAtLocation.plantTime
                Singleton<GameWorld>.Instance.MainPlayer.Profile.ItemDroppedAtPlace(conditionLeaveItemAtLocation.target.FirstOrDefault(), conditionLeaveItemAtLocation.zoneId);
            }
            else if (QuestCondition is ConditionPlaceBeacon conditionPlaceBeacon)
            {
                // 等待 conditionPlaceBeacon.plantTime
                Singleton<GameWorld>.Instance.MainPlayer.Profile.ItemDroppedAtPlace(conditionPlaceBeacon.target.FirstOrDefault(), conditionPlaceBeacon.zoneId);
            }
        }

        public bool IsProxyActionDisabled()
        {
            return QuestCondition switch
            {
                ConditionVisitPlace or
                ConditionLeaveItemAtLocation or
                ConditionPlaceBeacon => false,
                _ => true
            };
        }

        public string Id()
        {
            return QuestCondition switch
            {
                ConditionVisitPlace conditionVisitPlace => conditionVisitPlace.target,
                ConditionLeaveItemAtLocation conditionLeaveItemAtLocation => conditionLeaveItemAtLocation.zoneId,
                ConditionPlaceBeacon conditionPlaceBeacon => conditionPlaceBeacon.zoneId,
                _ => ""
            };
        }
    }
}