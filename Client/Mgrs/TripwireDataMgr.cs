

using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class TripwireDataMgr : DataMgr<TripwireDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (!Tools.IsHost)
            {
                return;
            }
            LoadData(LoadTripwires);
            StartCoroutine(ReloadDataLoop(2f, LoadTripwires));
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
                        _datas.Add(data);
                    }
                }
            }
            _datas = datas;
        }
    }
}