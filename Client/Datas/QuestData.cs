
using System.Linq;
using System.Threading.Tasks;
using EFT;
using EFT.Quests;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class QuestData : TriggerData, IProxyActor
    {
        private QuestStatusData _questRef;
        public QuestStatusData Quest => _questRef;
        private Condition _conditionRef;
        public Condition QuestCondition => _conditionRef;
        private Condition _parentConditionRef;
        public Condition QuestParentCondition => _parentConditionRef;
        private Transform _transformRef;
        private Transform _questTransform => _transformRef;
        private string _zoneId;
        private string _targetId;
        public string ZoneId => _zoneId;
        public string TargetId => _targetId;
        private QuestDataMgr QuestDataMgr => field ??= MgrAccessor.Get<QuestDataMgr>();

        public QuestData(QuestStatusData quest, Transform questTransform, Condition condition, string zoneId, string targetId, Condition parentCondition = null) : base()
        {
            _conditionRef = condition;
            _parentConditionRef = parentCondition;
            _questRef = quest;
            _transformRef = questTransform;
            _zoneId = zoneId;
            _targetId = targetId;
            _colliders = questTransform.GetComponentsInChildren<Collider>().ToList();
        }

        public override string GetActionName()
        {
            var condition = QuestParentCondition != null ? QuestParentCondition : QuestCondition;
            return GetConditionText(condition);
        }

        private static string GetConditionText(Condition condition)
        {
            if (condition == null)
            {
                return string.Empty;
            }

            try
            {
                var text = condition.FormattedDescription;
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }
            catch
            {
            }

            try
            {
                return condition.id.ToString().McsLocalized();
            }
            catch
            {
                return string.Empty;
            }
        }

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, _questTransform.position)));
        
        public override bool IsDisabled() => false;

        public override void Dispose()
        {
            base.Dispose();
            _questRef = null;
            _conditionRef = null;
            _parentConditionRef = null;
            _transformRef = null;
        }

        public virtual async Task ForceCompleteQuest(Player mcsBotPlayer)
        {
            await QuestDataMgr.ForceCompleteCondition(mcsBotPlayer, this);

            // if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            // {
            //     EventMgr.Notify(new CommandMgrHandleFikaEvent
            //     {
            //         McsBotPlayer = mcsBotPlayer,
            //         CommandPacketType = ECommandType.EndProxyAction.ToString()
            //     });
            // }
            // else
            // {
            //     var mcsBotPlayerData = mcsBotPlayer.AIData.BotOwner.GetMcsBotPlayerData();
            //     if (mcsBotPlayerData != null)
            //     {
            //         mcsBotPlayerData.RemoveIntent([Intents.ShouldInteractionProxyAction, Intents.ShouldQuestProxyAction, Intents.ShouldLootProxyAction, Intents.ShouldHoldPosition]);
            //         mcsBotPlayerData.TargetPos = null;
            //         mcsBotPlayerData.ProxyTargetId = null;
            //     }
            // }

            var mcsBotPlayerData = mcsBotPlayer.AIData.BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.RemoveIntent([Intents.ShouldInteractionProxyAction, Intents.ShouldQuestProxyAction, Intents.ShouldLootProxyAction, Intents.ShouldHoldPosition]);
                mcsBotPlayerData.TargetPos = null;
                mcsBotPlayerData.ProxyTargetId = null;
            }
        }

        public virtual bool IsProxyActionDisabled()
        {
            var condition = QuestCondition;
            if (condition == null)
            {
                return true;
            }

            if (condition.TryCast<ConditionVisitPlace>() != null
                || condition.TryCast<ConditionLeaveItemAtLocation>() != null
                || condition.TryCast<ConditionPlaceBeacon>() != null)
            {
                return false;
            }

            return true;
        }

        public virtual string Id()
        {
            return $"{_zoneId}|{_targetId}";
        }
    }
}