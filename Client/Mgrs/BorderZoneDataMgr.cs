

using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class BorderZoneDataMgr : DataMgr<BorderZoneDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (!Tools.IsHost)
            {
                return;
            }
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