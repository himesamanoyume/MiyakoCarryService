using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class BorderZoneExtensions
    {

        extension(BorderZone borderZone)
        {
            public BorderZoneData GetData()
            {
                return InteropDataCache<BorderZoneData>.GetOrCreate(borderZone.Pointer, () => new BorderZoneData(borderZone));
            }
        }

        public static void Clear()
        {
            InteropDataCache<BorderZoneData>.Clear();
        }
    }
}