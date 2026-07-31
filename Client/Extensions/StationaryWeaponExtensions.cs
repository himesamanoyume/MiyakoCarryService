
using System.Runtime.CompilerServices;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Extensions
{
    public static class StationaryWeaponExtensions
    {
        private static readonly ConditionalWeakTable<StationaryWeapon, StationaryWeaponData> _dataDict = new();
        
        extension(StationaryWeapon stationaryWeapon)
        {
            public StationaryWeaponData GetData()
            {
                return _dataDict.TryGetValue(stationaryWeapon, out StationaryWeaponData data) ? data : stationaryWeapon.InitData();
            }

            public StationaryWeaponData InitData()
            {
                var data = new StationaryWeaponData(stationaryWeapon);
                _dataDict.Add(stationaryWeapon, data);
                return data;
            }
        }
    }
}