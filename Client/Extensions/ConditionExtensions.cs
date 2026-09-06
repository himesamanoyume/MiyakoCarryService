
using EFT.Quests;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Extensions
{
    public static class ConditionExtensions
    {
        private static readonly AttachedTable<Condition, QuestData> _datas = new();

        extension(Condition condition)
        {
            public QuestData GetData(QuestDataClass questDataClass, Transform transform, Condition parentCondition = null)
            {
                return _datas.GetOrCreate(condition, key => new QuestData(questDataClass, transform, key, parentCondition));
            }
        }
    }
}