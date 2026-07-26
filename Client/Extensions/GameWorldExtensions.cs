using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class GameWorldExtensions
    {
        private static SwitchDataMgr SwitchDataMgr => MgrAccessor.Get<SwitchDataMgr>();
        private static DoorDataMgr DoorDataMgr => MgrAccessor.Get<DoorDataMgr>();
        private static StationaryWeaponDataMgr StationaryWeaponDataMgr => MgrAccessor.Get<StationaryWeaponDataMgr>();

        extension(GameWorld gameWorld)
        {
            public InteractableObjectData FindInteractableObjectData(string id)
            {
                if (string.IsNullOrEmpty(id))
                {
                    return null;
                }
                var doorData = DoorDataMgr.FindDoor(id);
                if (doorData != null)
                {
                    return doorData;
                }

                var switchData = SwitchDataMgr.FindSwitch(id);
                if (switchData != null)
                {
                    return switchData;
                }

                var stationaryWeaponData = StationaryWeaponDataMgr.FindStationaryWeapon(id);
                if (stationaryWeaponData != null)
                {
                    return stationaryWeaponData;
                }
                return null;
            }
        }
    }
}