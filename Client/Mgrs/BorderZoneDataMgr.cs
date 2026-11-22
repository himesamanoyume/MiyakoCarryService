

using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using System;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class BorderZoneDataMgr : DataMgr
    {
        public BorderZoneDataMgr(IntPtr pointer) : base(pointer)
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