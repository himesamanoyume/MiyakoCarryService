

using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class BorderZoneDataMgr : DataMgr<BorderZoneDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadBorderZone);
        }

        private void LoadBorderZone()
        {
            var borderZones = LocationScene.GetAllObjects<BorderZone>();
            foreach (var borderZone in borderZones)
            {
                var data = borderZone.GetData();
                if (data != null)
                {
                    _datas.Add(data);
                }
            }
        }
    }
}