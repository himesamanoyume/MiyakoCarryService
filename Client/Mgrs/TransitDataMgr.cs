
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class TransitDataMgr : GameWorldDataMgr<TransitDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            LoadData(LoadTransitPoints);
        }

        private void LoadTransitPoints()
        {
            foreach (var transitPoint in LocationScene.GetAllObjects<TransitPoint>())
            {
                _datas.Add(transitPoint.GetData());
            }
        }
    }
}