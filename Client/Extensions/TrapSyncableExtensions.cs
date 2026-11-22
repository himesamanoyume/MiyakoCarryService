
using CommonAssets.Scripts.Game.LabyrinthEvent;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TrapSyncableExtensions
    {

        extension(TrapSyncable trapSyncable)
        {
            public BarbedWireData GetData()
            {
                return InteropDataCache<BarbedWireData>.GetOrCreate(trapSyncable.Pointer, () => new BarbedWireData(trapSyncable));
            }
        }

        public static void Clear()
        {
            InteropDataCache<BarbedWireData>.Clear();
        }
    }
}