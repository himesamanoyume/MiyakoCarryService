
using System;
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
            _collider = questTransform.GetComponent<Collider>();
        }

        public override string GetActionName() => QuestCondition.id.ToString().McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, _questTransform.position)));
        
        public override bool IsDisabled() => false;

        public bool IsProxyActionAllowed()
        {
            return QuestCondition switch
            {
                ConditionVisitPlace or
                ConditionLaunchFlare or
                ConditionZone or
                ConditionInZone => true,
                _ => false
            };
        }

        public void ExcuteProxyAction()
        {
            switch (QuestCondition)
            {
                case ConditionVisitPlace:

                    break;
                case ConditionLaunchFlare:

                    break;
                case ConditionZone:

                    break;
                case ConditionInZone:

                    break;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _questRef = null;
            _conditionRef = null;
            _transformRef = null;
        }
    }
}