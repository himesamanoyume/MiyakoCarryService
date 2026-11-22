using System.Collections.Generic;
using EFT.Interactive;
using EFT.Quests;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using Comfort.Common;
using EFT;
using System.Linq;
using MiyakoCarryService.Client.Utils;
using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class QuestDataMgr : GameWorldDataMgr
    {
        public QuestDataMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        private List<TriggerWithId> _triggersWithIds;
        private Dictionary<string, List<TriggerWithId>> _triggersById;
        private ConcurrentDictionary<Type, Func<Player, Condition, QuestData, Task>> _forceCompleteFuncMap = null;

        protected override void OnMgrStart()
        {
            base.OnMgrStart();
            InitFuncMap();
        }

        public virtual void InitFuncMap()
        {
            if (_forceCompleteFuncMap == null)
            {
                _forceCompleteFuncMap = new();
            }

            RegisterFuncMap(typeof(ConditionLeaveItemAtLocation), ForceCompleteConditionLeaveItemAtLocation);
            RegisterFuncMap(typeof(ConditionPlaceBeacon), ForceCompleteConditionPlaceBeacon);
            RegisterFuncMap(typeof(ConditionVisitPlace), ForceCompleteConditionVisitPlace);
        }

        public void RegisterFuncMap(Type conditionType, Func<Player, Condition, QuestData, Task> func)
        {
            if (_forceCompleteFuncMap == null)
            {
                _forceCompleteFuncMap = new();
            }
            _forceCompleteFuncMap.AddOrUpdate(conditionType, _condition => func,
                (_condition, oldFunc) =>
                {
                    oldFunc = func;
                    return oldFunc;
                }
            );
        }

        public async Task ForceCompleteCondition(Player player, QuestData questData)
        {
            var condition = questData?.QuestCondition;
            if (condition == null || _forceCompleteFuncMap == null)
            {
                return;
            }

            foreach (var func in _forceCompleteFuncMap.Values)
            {
                await func.Invoke(player, condition, questData);
            }
        }

        public virtual async Task ForceCompleteConditionLeaveItemAtLocation(Player player, Condition condition, QuestData questData)
        {
            var conditionLeaveItemAtLocation = condition == null ? null : condition.TryCast<ConditionLeaveItemAtLocation>();
            if (conditionLeaveItemAtLocation != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(conditionLeaveItemAtLocation.plantTime));
                Singleton<GameWorld>.Instance.MainPlayer.Profile.ItemDroppedAtPlace(questData.TargetId, questData.ZoneId);
            }
        }

        public virtual async Task ForceCompleteConditionPlaceBeacon(Player player, Condition condition, QuestData questData)
        {
            var conditionPlaceBeacon = condition == null ? null : condition.TryCast<ConditionPlaceBeacon>();
            if (conditionPlaceBeacon != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(conditionPlaceBeacon.plantTime));
                Singleton<GameWorld>.Instance.MainPlayer.Profile.ItemDroppedAtPlace(questData.TargetId, questData.ZoneId);
            }
        }

        public virtual async Task ForceCompleteConditionVisitPlace(Player player, Condition condition, QuestData questData)
        {
            var conditionVisitPlace = condition == null ? null : condition.TryCast<ConditionVisitPlace>();
            if (conditionVisitPlace != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            BuildTriggerIndex();
            LoadData(LoadQuestData);
            StartCoroutine(ReloadDataLoop(10f, LoadQuestData).McsSafe());
        }

        private void BuildTriggerIndex()
        {
            _triggersWithIds = FindObjectsOfType<TriggerWithId>().ToList();
            _triggersById = new Dictionary<string, List<TriggerWithId>>();
            foreach (var triggerWithId in _triggersWithIds)
            {
                if (triggerWithId == null)
                {
                    continue;
                }

                var id = triggerWithId.Id;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!_triggersById.TryGetValue(id, out var triggers))
                {
                    triggers = new List<TriggerWithId>();
                    _triggersById[id] = triggers;
                }

                triggers.Add(triggerWithId);
            }
        }

        private List<TriggerWithId> GetTriggersById(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || _triggersById == null)
            {
                return null;
            }

            return _triggersById.TryGetValue(zoneId, out var triggers) ? triggers : null;
        }

        private static bool IsDrawableQuest(QuestStatusData questStatusData)
        {
            if (!IsQuestStatusDrawable(questStatusData))
            {
                return false;
            }

            return HasDisplayableName(questStatusData);
        }

        private static bool IsQuestStatusDrawable(QuestStatusData questStatusData)
        {
            if (questStatusData == null || questStatusData.Template == null)
            {
                return false;
            }

            if (questStatusData.Status == EQuestStatus.Started)
            {
                return true;
            }

            return questStatusData.Template.IsMainQuest && questStatusData.Status == EQuestStatus.AutoStart;
        }

        private static bool HasDisplayableName(QuestStatusData questStatusData)
        {
            if (questStatusData == null)
            {
                return false;
            }

            var questId = questStatusData.Id;

            if (IsRealName(GetRuntimeQuestName(questStatusData), questId))
            {
                return true;
            }

            if (IsRealName(questStatusData.Template?.Name, questId))
            {
                return true;
            }

            var nameKey = questId + " name";
            return IsRealName(nameKey.McsLocalized(), questId);
        }

        private static string GetRuntimeQuestName(QuestStatusData questStatusData)
        {
            try
            {
                var quest = questStatusData.Quest;
                return quest == null ? null : quest.Name;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsRealName(string name, string questId)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return name != questId && name != questId + " name";
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            if (_triggersWithIds != null)
            {
                _triggersWithIds.Clear();
            }
            _triggersWithIds = null;
            _triggersById = null;
        }

        /// <summary>
        /// 需要开放
        /// </summary>
        private void LoadQuest(HashSet<QuestData> datas, Condition condition, QuestStatusData quest, Condition parentCondition = null)
        {
            if (_triggersById == null || condition == null)
            {
                return;
            }

            var conditionZone = condition.TryCast<ConditionZone>();
            if (conditionZone != null)
            {
                foreach (var zoneId in conditionZone.zoneIds)
                {
                    var zones = GetTriggersById(zoneId);
                    if (zones == null)
                    {
                        continue;
                    }

                    foreach (var zone in zones)
                    {
                        if (zone.transform == null)
                        {
                            continue;
                        }

                        var targets = conditionZone.target;
                        var targetCount = targets == null ? 0 : targets.Length;
                        if (targetCount == 0)
                        {
                            var data = condition.GetData(quest, zone.transform, zoneId, null, parentCondition);
                            if (data != null)
                            {
                                datas.Add(data);
                            }
                            continue;
                        }

                        for (var i = 0; i < targetCount; i++)
                        {
                            var targetId = targets[i];
                            var data = condition.GetData(quest, zone.transform, zoneId, targetId, parentCondition);
                            if (data != null)
                            {
                                datas.Add(data);
                            }
                        }
                    }
                }
                return;
            }

            var conditionLaunchFlare = condition.TryCast<ConditionLaunchFlare>();
            if (conditionLaunchFlare != null)
            {
                var zones = GetTriggersById(conditionLaunchFlare.zoneID);
                if (zones == null)
                {
                    return;
                }

                foreach (var zone in zones)
                {
                    if (zone.transform == null)
                    {
                        continue;
                    }
                    var data = condition.GetData(quest, zone.transform, conditionLaunchFlare.zoneID, null, parentCondition);
                    if (data != null)
                    {
                        datas.Add(data);
                    }
                }
                return;
            }

            var conditionVisitPlace = condition.TryCast<ConditionVisitPlace>();
            if (conditionVisitPlace != null)
            {
                var zones = GetTriggersById(conditionVisitPlace.target);
                if (zones == null)
                {
                    return;
                }

                foreach (var zone in zones)
                {
                    if (zone.transform == null)
                    {
                        continue;
                    }
                    var data = condition.GetData(quest, zone.transform, conditionVisitPlace.target, null, parentCondition);
                    if (data != null)
                    {
                        datas.Add(data);
                    }
                }
                return;
            }

            var conditionExitName = condition.TryCast<ConditionExitName>();
            if (conditionExitName != null)
            {
                var exfilDataMgr = MgrAccessor.Get<ExfilDataMgr>();
                if (exfilDataMgr == null)
                {
                    return;
                }

                ExfilData specifiedExfil = null;

                foreach (var exfilData in exfilDataMgr.GetDatas<ExfilData>())
                {
                    if (exfilData.ExfiltrationPoint != null &&
                        exfilData.ExfiltrationPoint.Settings != null &&
                        exfilData.ExfiltrationPoint.Settings.Name == conditionExitName.exitName)
                    {
                        specifiedExfil = exfilData;
                        break;
                    }
                }

                if (specifiedExfil != null)
                {
                    if (specifiedExfil.ExfiltrationPoint.transform.transform == null)
                    {
                        return;
                    }
                    var data = condition.GetData(quest, specifiedExfil.ExfiltrationPoint.transform, conditionExitName.exitName, null, parentCondition);
                    if (data != null)
                    {
                        datas.Add(data);
                    }
                }
                return;
            }

            var conditionInZone = condition.TryCast<ConditionInZone>();
            if (conditionInZone != null)
            {
                foreach (var zoneId in conditionInZone.zoneIds)
                {
                    var zones = GetTriggersById(zoneId);
                    if (zones == null)
                    {
                        continue;
                    }

                    foreach (var zone in zones)
                    {
                        if (zone.transform == null)
                        {
                            continue;
                        }
                        var data = condition.GetData(quest, zone.transform, zoneId, null, parentCondition);
                        if (data != null)
                        {
                            datas.Add(data);
                        }
                    }
                }

                return;
            }

            var conditionCounterCreator = condition.TryCast<ConditionCounterCreator>();
            if (conditionCounterCreator != null)
            {
                foreach (var _condition in conditionCounterCreator.Conditions)
                {
                    LoadQuest(datas, _condition, quest, conditionCounterCreator);
                }
            }
        }

        private void LoadQuestData()
        {
            var myPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (myPlayer == null)
            {
                return;
            }

            var questsData = myPlayer.Profile?.QuestsData;
            if (questsData == null)
            {
                return;
            }

            var startedQuests = new List<QuestStatusData>();
            foreach (var questStatusData in questsData)
            {
                if (IsDrawableQuest(questStatusData))
                {
                    startedQuests.Add(questStatusData);
                }
            }

            if (startedQuests.Count == 0)
            {
                return;
            }

            if (_triggersById == null)
            {
                BuildTriggerIndex();
            }

            var datas = new HashSet<QuestData>();
            foreach (var quest in startedQuests)
            {
                try
                {
                    var template = quest.Template;
                    if (template == null)
                    {
                        continue;
                    }

                    if (template.Conditions == null || !template.Conditions.ContainsKey(EQuestStatus.AvailableForFinish))
                    {
                        continue;
                    }

                    var completedConditions = quest.CompletedConditions;

                    foreach (var condition in template.Conditions[EQuestStatus.AvailableForFinish])
                    {
                        if (completedConditions != null && completedConditions.Contains(condition.id))
                        {
                            continue;
                        }

                        LoadQuest(datas, condition, quest);
                    }
                }
                catch
                {
                }
            }

            var dataLeft = _datas.Except(datas).ToList();
            var dataJoined = datas.Except(_datas).ToList();
            foreach (var data in dataLeft)
            {
                _datas.Remove(data);
            }
            foreach (var data in dataJoined)
            {
                _datas.Add(data);
            }
        }

        public Dictionary<QuestStatusData, List<QuestData>> GetQuestDataByGroup()
        {
            var questDataGroup = new Dictionary<QuestStatusData, List<QuestData>>();
            foreach (QuestData questData in _datas)
            {
                if (!questDataGroup.ContainsKey(questData.Quest))
                {
                    questDataGroup.Add(questData.Quest, new() { questData });
                    continue;
                }

                if (questDataGroup.TryGetValue(questData.Quest, out var questDatas))
                {
                    questDatas.Add(questData);
                }
            }
            return questDataGroup;
        }

        public QuestData FindQuestData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            foreach (QuestData questData in _datas)
            {
                if (questData.Id() == id)
                {
                    return questData;
                }
            }
            return null;
        }
    }
}
