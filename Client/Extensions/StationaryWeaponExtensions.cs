
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class StationaryWeaponExtensions
    {
        private static readonly AttachedTable<StationaryWeapon, StationaryWeaponData> _datas = new();

        extension(StationaryWeapon stationaryWeapon)
        {
            public StationaryWeaponData GetData()
            {
                return _datas.GetOrCreate(stationaryWeapon, key => new StationaryWeaponData(key));
            }
        }
    }
}