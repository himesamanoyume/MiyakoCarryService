

using CommonAssets.Scripts.Game.LabyrinthEvent;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public class BarbedWireDataMgr : LabyrinthDataMgr<BarbedWireDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (!Tools.IsHost)
            {
                return;
            }
            if (!_shouldInit)
            {
                return;
            }
            LoadData(LoadBarbedWire);
        }

        private void LoadBarbedWire()
        {
            var syncables = LocationScene.GetAllObjects<ISyncAble>();
            foreach (var syncable in syncables)
            {
                if (syncable is not TrapSyncable trap)
                {
                    continue;
                }

                if (trap.TrapType == ETrapType.BarbedWire && trap.gameObject.activeSelf)
                {
                    var data = trap.GetData();
                    if (data != null)
                    {
                        _datas.Add(data);
                    }
                }
            }
        }
    }
}