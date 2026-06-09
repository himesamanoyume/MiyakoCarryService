

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
            public QuestData GetData(QuestDataClass questDataClass, Transform transform)
            {
                return _dataDict.TryGetValue(condition, out QuestData data) ? data : condition.InitData(questDataClass, transform);
            }

            public QuestData InitData(QuestDataClass questDataClass, Transform transform)
            {
                var data = new QuestData(questDataClass, transform, condition);
                _dataDict.Add(condition, data);
                return data;
            }
        }
    }
}