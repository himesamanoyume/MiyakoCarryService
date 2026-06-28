using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class GameWorldExtensions
    {
        private static SwitchDataMgr SwitchDataMgr = MgrAccessor.Get<SwitchDataMgr>();

        extension(GameWorld gameWorld)
        {
            public InteractableObjectData FindInteractableObjectData(string id)
            {
                if (string.IsNullOrEmpty(id))
                {
                    return null;
                }
                var door = gameWorld.FindDoor(id);
                if (door != null && door is Door _door)
                {
                    return _door.GetData();
                }

                var switchData = SwitchDataMgr.FindSwitch(id);
                if (switchData != null)
                {
                    return switchData;
                }
                return null;
            }
        }
    }
}