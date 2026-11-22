using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using System;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class DoorDataMgr : GameWorldDataMgr
    {
        public DoorDataMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public override void OnRaidStarted()
        {
            LoadData(LoadDoors);
        }

        private void LoadDoors()
        {
            var world = Singleton<GameWorld>.Instance.World;
            List<Door> doors;
            if (world == null)
            {
                doors = FindObjectsOfType<Door>().ToList();
            }
            else
            {
                doors = new List<Door>();
                foreach (var worldInteractiveObject in world.WorldInteractiveObjects())
                {
                    var door = worldInteractiveObject.TryCast<Door>();
                    if (door != null)
                    {
                        doors.Add(door);
                    }
                }
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