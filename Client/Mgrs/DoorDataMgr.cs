

using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class DoorDataMgr : GameWorldDataMgr<DoorDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            LoadData(LoadDoors);
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            OnRaidEnded();
        }

        private void LoadDoors()
        {
            var doors = Singleton<GameWorld>.Instance.World_0.WorldInteractiveObjects().OfType<Door>();
            foreach (var door in doors)
            {
                _datas.Add(door.GetData());
            }
        }
    }
}