using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class BorderZoneExtensions
    {
        private static readonly AttachedTable<BorderZone, BorderZoneData> _datas = new();

        extension(BorderZone borderZone)
        {
            public BorderZoneData GetData()
            {
                return _datas.GetOrCreate(borderZone, key => new BorderZoneData(key));
            }
        }
    }
}