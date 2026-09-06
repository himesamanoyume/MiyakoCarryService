
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class DamageTriggerExtensions
    {
        private static readonly AttachedTable<DamageTrigger, DamageTriggerData> _datas = new();

        extension(DamageTrigger damageTrigger)
        {
            public DamageTriggerData GetData()
            {
                return _datas.GetOrCreate(damageTrigger, key => new DamageTriggerData(key));
            }
        }
    }
}