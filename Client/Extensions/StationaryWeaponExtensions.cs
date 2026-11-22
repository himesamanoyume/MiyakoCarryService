
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class StationaryWeaponExtensions
    {

        extension(StationaryWeapon stationaryWeapon)
        {
            public StationaryWeaponData GetData()
            {
                return InteropDataCache<StationaryWeaponData>.GetOrCreate(stationaryWeapon.Pointer, () => new StationaryWeaponData(stationaryWeapon));
            }
        }

        public static void Clear()
        {
            InteropDataCache<StationaryWeaponData>.Clear();
        }
    }
}