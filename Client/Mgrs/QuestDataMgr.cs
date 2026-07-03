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

namespace MiyakoCarryService.Client.Mgrs
{
    public class QuestDataMgr : GameWorldDataMgr
    {
        private List<TriggerWithId> _triggersWithIds;
        private ConcurrentDictionary<Type, Func<Player, Condition, Task>> _forceCompleteFuncMap = null;

        public override void Start()
        {
            base.Start();

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

        public void RegisterFuncMap(Type conditionType, Func<Player, Condition, Task> func)
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

        public async Task ForceCompleteCondition(Type type, Player player, Condition condition)
        {
            if (_forceCompleteFuncMap.TryGetValue(type, out var func))
            {
                await func.Invoke(player, condition);
            }
        }

        public virtual async Task ForceCompleteConditionLeaveItemAtLocation(Player player, Condition condition)
        {
            if (condition is ConditionLeaveItemAtLocation conditionLeaveItemAtLocation)
            {
                await Task.Delay(TimeSpan.FromSeconds(conditionLeaveItemAtLocation.plantTime));
                Singleton<GameWorld>.Instance.MainPlayer.Profile.ItemDroppedAtPlace(conditionLeaveItemAtLocation.target.FirstOrDefault(), conditionLeaveItemAtLocation.zoneId);
            }
        }

        public virtual async Task ForceCompleteConditionPlaceBeacon(Player player, Condition condition)
        {
            if (condition is ConditionPlaceBeacon conditionPlaceBeacon)
            {
                await Task.Delay(TimeSpan.FromSeconds(conditionPlaceBeacon.plantTime));
                Singleton<GameWorld>.Instance.MainPlayer.Profile.ItemDroppedAtPlace(conditionPlaceBeacon.target.FirstOrDefault(), conditionPlaceBeacon.zoneId);
            }
        }

        public virtual async Task ForceCompleteConditionVisitPlace(Player player, Condition condition)
        {
            if (condition is ConditionVisitPlace conditionVisitPlace)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadQuestData);
            StartCoroutine(ReloadDataLoop(10f, LoadQuestData));
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            if (_triggersWithIds != null)
            {
                _triggersWithIds.Clear();
            }
            _triggersWithIds = null;
        }

        /// <summary>
        /// 需要开放
        /// </summary>
        private void LoadQuest(HashSet<QuestData> datas, Condition condition, QuestDataClass quest, Condition parentCondition = null)
        {
            if (_triggersWithIds == null)
            {
                return;
            }

            switch (condition)
            {
                case ConditionZone conditionZone:
                    {
                        var zones = new List<TriggerWithId>();
                        foreach (var t in _triggersWithIds)
                        {
                            if (t.Id == conditionZone.zoneId)
                            {
                                zones.Add(t);
                            }
                        }
                        foreach (var zone in zones)
                        {
                            if (zone.transform == null)
                            {
                                continue;
                            }

                            var data = condition.GetData(quest, zone.transform, parentCondition);
                            if (data != null)
                            {
                                datas.Add(data);
                            }
                        }
                        break;
                    }
                case ConditionLaunchFlare conditionLaunchFlare:
                    {
                        var zones = new List<TriggerWithId>();
                        foreach (var t in _triggersWithIds)
                        {
                            if (t.Id == conditionLaunchFlare.zoneID)
                            {
                                zones.Add(t);
                            }
                        }
                        foreach (var zone in zones)
                        {
                            if (zone.transform == null)
                            {
                                continue;
                            }
                            var data = condition.GetData(quest, zone.transform, parentCondition);
                            if (data != null)
                            {
                                datas.Add(data);
                            }
                        }
                        break;
                    }
                case ConditionVisitPlace conditionVisitPlace:
                    {
                        var zones = new List<TriggerWithId>();
                        foreach (var t in _triggersWithIds)
                        {
                            if (t.Id == conditionVisitPlace.target)
                            {
                                zones.Add(t);
                            }
                        }
                        foreach (var zone in zones)
                        {
                            if (zone.transform == null)
                            {
                                continue;
                            }
                            var data = condition.GetData(quest, zone.transform, parentCondition);
                            if (data != null)
                            {
                                datas.Add(data);
                            }
                        }
                        break;
                    }
                case ConditionExitName conditionExitName:
                    {
                        var exfilDataMgr = MgrAccessor.Get<ExfilDataMgr>();
                        if (exfilDataMgr == null)
                        {
                            break;
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
                                break;
                            }
                            var data = condition.GetData(quest, specifiedExfil.ExfiltrationPoint.transform, parentCondition);
                            if (data != null)
                            {
                                datas.Add(data);
                            }
                        }
                        break;
                    }
                case ConditionInZone conditionInZone:
                    {
                        foreach (var zoneId in conditionInZone.zoneIds)
                        {
                            var zones = new List<TriggerWithId>();
                            foreach (var t in _triggersWithIds)
                            {
                                if (t.Id == zoneId)
                                {
                                    zones.Add(t);
                                }
                            }
                            foreach (var zone in zones)
                            {
                                if (zone.transform == null)
                                {
                                    continue;
                                }
                                var data = condition.GetData(quest, zone.transform, parentCondition);
                                if (data != null)
                                {
                                    datas.Add(data);
                                }
                            }
                        }

                        break;
                    }
                case ConditionCounterCreator conditionCounterCreator:
                    {
                        foreach (var _condition in conditionCounterCreator.Conditions)
                        {
                            LoadQuest(datas, _condition, quest, conditionCounterCreator);
                        }
                        break;
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

            var datas = new HashSet<QuestData>();
            var startedQuests = new List<QuestDataClass>();
            if (myPlayer.Profile?.QuestsData != null)
            {
                foreach (var questDataClass in myPlayer.Profile.QuestsData)
                {
                    if (questDataClass.Status == EQuestStatus.Started && questDataClass.Template != null)
                    {
                        startedQuests.Add(questDataClass);
                    }
                }
            }

            if (startedQuests.Count == 0)
            {
                return;
            }

            if (_triggersWithIds == null)
            {
                _triggersWithIds = FindObjectsOfType<TriggerWithId>().ToList();
            }

            foreach (var quest in startedQuests)
            {
                if (quest.Template == null)
                {
                    continue;
                }

                foreach (var condition in quest.Template.Conditions[EQuestStatus.AvailableForFinish])
                {
                    if (quest.CompletedConditions.Contains(condition.id))
                    {
                        continue;
                    }

                    LoadQuest(datas, condition, quest);
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

        public Dictionary<QuestDataClass, List<QuestData>> GetQuestDataByGroup()
        {
            var questDataGroup = new Dictionary<QuestDataClass, List<QuestData>>();
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
