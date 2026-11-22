
using EFT.GameTriggers;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TriggerZoneExtensions
    {

        extension(TriggerZone triggerZone)
        {
            public TriggerZoneData GetData(string triggerZoneId)
            {
                return InteropDataCache<TriggerZoneData>.GetOrCreate(triggerZone.Pointer, () => new TriggerZoneData(triggerZone, triggerZoneId));
            }
        }

        public static void Clear()
        {
            InteropDataCache<TriggerZoneData>.Clear();
        }
    }
}