
using System.Runtime.CompilerServices;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Extensions
{
    public static class DamageTriggerExtensions
    {
        private static readonly ConditionalWeakTable<DamageTrigger, DamageTriggerData> _dataDict = new();
        
        extension(DamageTrigger damageTrigger)
        {
            public DamageTriggerData GetData()
            {
                return _dataDict.TryGetValue(damageTrigger, out DamageTriggerData data) ? data : damageTrigger.InitData();
            }

            public DamageTriggerData InitData()
            {
                var data = new DamageTriggerData(damageTrigger);
                _dataDict.Add(damageTrigger, data);
                return data;
            }
        }
    }
}