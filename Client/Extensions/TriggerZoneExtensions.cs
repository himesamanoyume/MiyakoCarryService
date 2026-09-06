
using EFT.GameTriggers;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TriggerZoneExtensions
    {
        private static readonly AttachedTable<TriggerZone, TriggerZoneData> _datas = new();

        extension(TriggerZone triggerZone)
        {
            public TriggerZoneData GetData(string triggerZoneId)
            {
                return _datas.GetOrCreate(triggerZone, key => new TriggerZoneData(key, triggerZoneId));
            }
        }
    }
}