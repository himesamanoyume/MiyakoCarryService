

using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public class TripwireDataMgr : DataMgr<TripwireDataMgr>
    {
        protected override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (!Tools.IsHost)
            {
                return;
            }
            LoadData(LoadTripwires);
            StartCoroutine(ReloadDataLoop(2f, LoadTripwires));
            StartCoroutine(ReloadDataLoop(1f, RefreshTripwireState));
        }

        private void LoadTripwires()
        {
            var datas = new HashSet<BaseData>();
            foreach (var synchronizableObject in Singleton<GameWorld>.Instance.SynchronizableObjectLogicProcessor.GetSynchronizableObjects())
            {
                if (synchronizableObject is TripwireSynchronizableObject tripwireSynchronizableObject)
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