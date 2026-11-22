
using EFT.Quests;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Extensions
{
    public static class ConditionExtensions
    {

        extension(Condition condition)
        {
            public QuestData GetData(QuestStatusData quest, Transform transform, string zoneId, string targetId, Condition parentCondition = null)
            {
                return InteropDataCache<QuestData>.GetOrCreate(condition.Pointer, zoneId, targetId, () => new QuestData(quest, transform, condition, zoneId, targetId, parentCondition));
            }
        }

        public static void Clear()
        {
            InteropDataCache<QuestData>.Clear();
        }
    }
}