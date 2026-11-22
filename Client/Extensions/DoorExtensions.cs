using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class DoorExtensions
    {

        extension(Door door)
        {
            public DoorData GetData()
            {
                return InteropDataCache<DoorData>.GetOrCreate(door.Pointer, () => new DoorData(door));
            }
        }

        public static void Clear()
        {
            InteropDataCache<DoorData>.Clear();
        }
    }
}