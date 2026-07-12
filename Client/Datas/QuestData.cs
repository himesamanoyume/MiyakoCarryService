
using System;
using System.Linq;
using System.Threading.Tasks;
using EFT;
using EFT.Quests;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Mgrs;
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
        private WeakReference<Condition> _parentConditionRef;
        public Condition QuestParentCondition => _parentConditionRef != null ? _parentConditionRef.TryGetTarget(out var parentCondition) ? parentCondition : null : null;
        private WeakReference<Transform> _transformRef;
        private Transform _questTransform => _transformRef.TryGetTarget(out var transform) ? transform : null;
        private QuestDataMgr QuestDataMgr => MgrAccessor.Get<QuestDataMgr>();

        public QuestData(QuestDataClass quest, Transform questTransform, Condition condition, Condition parentCondition = null) : base()
        {
            _conditionRef = new WeakReference<Condition>(condition);
            _parentConditionRef = new WeakReference<Condition>(parentCondition);
            _questRef = new WeakReference<QuestDataClass>(quest);
            _transformRef = new WeakReference<Transform>(questTransform);
            _colliders = questTransform.GetComponentsInChildren<Collider>().ToList();
        }

        public override string GetActionName() => QuestParentCondition != null ? QuestParentCondition.id.ToString().McsLocalized() : QuestCondition.id.ToString().McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, _questTransform.position)));
        
        public override bool IsDisabled() => false;

        public override void Dispose()
        {
            base.Dispose();
            _questRef = null;
            _conditionRef = null;
            _transformRef = null;
        }

        public virtual async Task ForceCompleteQuest(Player mcsBotPlayer)
        {
            await QuestDataMgr.ForceCompleteCondition(QuestCondition.GetType(), mcsBotPlayer, QuestCondition);

            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandType.EndProxyAction.ToString()
                });
            }
            else
            {
                var mcsBotPlayerData = mcsBotPlayer.AIData.BotOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData == null)
                {
                    mcsBotPlayerData.RemoveDecision([Decisions.ShouldInteractionProxyAction, Decisions.ShouldQuestProxyAction, Decisions.ShouldLootProxyAction, Decisions.ShouldHoldPosition]);
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
                }
            }
        }

        public virtual bool IsProxyActionDisabled()
        {
            return QuestCondition switch
            {
                ConditionVisitPlace or
                ConditionLeaveItemAtLocation or
                ConditionPlaceBeacon => false,
                _ => true
            };
        }

        public virtual string Id()
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