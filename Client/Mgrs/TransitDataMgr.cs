
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public class TransitDataMgr : GameWorldDataMgr<TransitDataMgr>
    {
        protected override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadTransitPoints);
        }

        private void LoadTransitPoints()
        {
            foreach (var transitPoint in LocationScene.GetAllObjects<TransitPoint>())
            {
                var data = transitPoint.GetData();
                if (data != null)
                {
                    _datas.Add(data);
                }
            }
        }
    }
}