using System.Collections.Generic;
using EFT.Interactive;
using EFT.Quests;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using Comfort.Common;
using EFT;
using System.Linq;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class QuestDataMgr : GameWorldDataMgr<QuestDataMgr>
    {
        private List<TriggerWithId> _triggersWithIds;
        // public HashSet<string> QuestNeedItemList { get; private set; } = new();

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadQuestData);
            StartCoroutine(ReloadDataLoop(10f, LoadQuestData));
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            if (_triggersWithIds != null)
            {
                _triggersWithIds.Clear();
            }
            _triggersWithIds = null;
        }

        private void LoadQuest(HashSet<QuestData> datas, Condition condition, QuestDataClass quest)
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

                            var data = condition.GetData(quest, zone.transform);
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
                            var data = condition.GetData(quest, zone.transform);
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
                            var data = condition.GetData(quest, zone.transform);
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
                            var data = condition.GetData(quest, specifiedExfil.ExfiltrationPoint.transform);
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
                                var data = condition.GetData(quest, zone.transform);
                                if (data != null)
                                {
                                    datas.Add(data);
                                }
                            }
                        }

                        break;
                    }
                // case ConditionFindItem conditionFindItem:
                //     {
                //         foreach (var questItemId in conditionFindItem.target)
                //         {
                //             if (!questNeedItemList.Contains(questItemId))
                //             {
                //                 questNeedItemList.Add(questItemId);
                //             }
                //         }
                //         break;
                //     }
                case ConditionCounterCreator conditionCounterCreator:
                    {
                        var zones = new List<TriggerWithId>();
                        foreach (var t in _triggersWithIds)
                        {
                            foreach (var _condition in conditionCounterCreator.Conditions)
                            {
                                if (_condition is ConditionVisitPlace conditionVisitPlace && t.Id == conditionVisitPlace.target)
                                {
                                    zones.Add(t);
                                    break;
                                }
                            }
                        }
                        
                        foreach (var zone in zones)
                        {
                            if (zone.transform == null)
                            {
                                continue;
                            }
                            var data = condition.GetData(quest, zone.transform);
                            if (data != null)
                            {
                                datas.Add(data);
                            }
                        }
                        break;
                    }
                // case ConditionItem conditionItem:
                //     {
                //         foreach (var questItemId in conditionItem.target)
                //         {
                //             if (!questNeedItemList.Contains(questItemId))
                //             {
                //                 questNeedItemList.Add(questItemId);
                //             }
                //         }
                //         break;
                //     }
                default:
                    {
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

            // var questNeedItemList = new HashSet<string>();

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

                    // LoadQuest(datas, questNeedItemList, condition, quest);
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
            // QuestNeedItemList = questNeedItemList;
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
    }
}
