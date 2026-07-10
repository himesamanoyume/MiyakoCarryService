using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public class DoorDataMgr : GameWorldDataMgr
    {
        public override void OnRaidStarted()
        {
            LoadData(LoadDoors);
        }

        private void LoadDoors()
        {
            var world = Singleton<GameWorld>.Instance.World_0;
            List<Door> doors;
            if (world == null)
            {
                doors = FindObjectsOfType<Door>().ToList();
            }
            else
            {
                doors = world.WorldInteractiveObjects().OfType<Door>().ToList();
            }
            foreach (var door in doors)
            {
                _datas.Add(door.GetData());
            }
        }

        public DoorData FindDoor(string doorId)
        {
            if (string.IsNullOrEmpty(doorId))
            {
                return null;
            }

            foreach (DoorData doorData in _datas)
            {
                if (doorData.Door.Id == doorId)
                {
                    return doorData;
                }
            }
            return null;
        }
    }
}