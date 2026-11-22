
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class DamageTriggerExtensions
    {

        extension(DamageTrigger damageTrigger)
        {
            public DamageTriggerData GetData()
            {
                return InteropDataCache<DamageTriggerData>.GetOrCreate(damageTrigger.Pointer, () => new DamageTriggerData(damageTrigger));
            }
        }

        public static void Clear()
        {
            InteropDataCache<DamageTriggerData>.Clear();
        }
    }
}