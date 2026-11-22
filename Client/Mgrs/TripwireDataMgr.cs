using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using System;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class TripwireDataMgr : DataMgr
    {
        public TripwireDataMgr(IntPtr pointer) : base(pointer)
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
            LoadData(LoadTripwires);
            StartCoroutine(ReloadDataLoop(2f, LoadTripwires).McsSafe());
            StartCoroutine(ReloadDataLoop(1f, RefreshTripwireState).McsSafe());
        }

        private void LoadTripwires()
        {
            var datas = new HashSet<BaseData>();
            foreach (var synchronizableObject in Singleton<GameWorld>.Instance.SynchronizableObjectLogicProcessor.GetSynchronizableObjects())
            {
                var tripwireSynchronizableObject = synchronizableObject == null ? null : synchronizableObject.TryCast<TripwireSynchronizableObject>();
                if (tripwireSynchronizableObject != null)
                {
                    var data = tripwireSynchronizableObject.GetData();
                    if (data != null)
                    {
                        datas.Add(data);
                    }
                }
            }
            _datas = datas;
        }

        private void RefreshTripwireState()
        {
            foreach (TripwireData tripwireData in _datas)
            {
                if (tripwireData.Tripwire.TripwireState is ETripwireState.Wait or ETripwireState.Active or ETripwireState.Exploding)
                {
                    tripwireData.SetObstacle(true);
                }
                else
                {
                    tripwireData.SetObstacle(false);
                }
            }
        } 
    }
}