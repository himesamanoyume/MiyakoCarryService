using CommonAssets.Scripts.Game.LabyrinthEvent;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using System;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class BarbedWireDataMgr : LabyrinthDataMgr
    {
        public BarbedWireDataMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public override void OnRaidStarted()
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
                var trap = syncable == null ? null : syncable.TryCast<TrapSyncable>();
                if (trap == null)
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