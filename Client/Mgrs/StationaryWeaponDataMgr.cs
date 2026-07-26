
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public class StationaryWeaponDataMgr : GameWorldDataMgr
    {
        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadStationaryWeapons);
        }

        private void LoadStationaryWeapons()
        {
            var stationaryWeapons = LocationScene.GetAllObjects<StationaryWeapon>(false);
            foreach (var stationaryWeapon in stationaryWeapons)
            {
                var data = stationaryWeapon.GetData();
                if (data != null)
                {
                    _datas.Add(data);
                }
            }
        }

        public StationaryWeaponData FindStationaryWeapon(string switchId)
        {
            if (string.IsNullOrEmpty(switchId))
            {
                return null;
            }

            foreach (StationaryWeaponData stationaryWeaponData in _datas)
            {
                if (stationaryWeaponData.StationaryWeapon.Id == switchId)
                {
                    return stationaryWeaponData;
                }
            }
            return null;
        }
    }
}