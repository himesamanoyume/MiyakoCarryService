

using System.Runtime.CompilerServices;
using EFT.Quests;
using MiyakoCarryService.Client.Datas;
using UnityEngine;

namespace MiyakoCarryService.Client.Extensions
{
    public static class ConditionExtensions
    {
        private static readonly ConditionalWeakTable<Condition, QuestData> _dataDict = new();
        
        extension(Condition condition)
        {
            public QuestData GetData(QuestDataClass questDataClass, Transform transform, Condition parentCondition = null)
            {
                return _dataDict.TryGetValue(condition, out QuestData data) ? data : condition.InitData(questDataClass, transform, parentCondition);
            }

            public QuestData InitData(QuestDataClass questDataClass, Transform transform, Condition parentCondition = null)
            {
                var data = new QuestData(questDataClass, transform, condition, parentCondition);
                _dataDict.Add(condition, data);
                return data;
            }
        }
    }
}