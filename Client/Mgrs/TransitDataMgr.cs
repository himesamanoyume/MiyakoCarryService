
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class TransitDataMgr : TriggerDataMgr<TransitDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            LoadData(LoadTransitPoints);
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
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