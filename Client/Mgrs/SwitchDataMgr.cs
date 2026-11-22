
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;
using System;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class SwitchDataMgr : GameWorldDataMgr
    {
        public SwitchDataMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadSwitches);
        }

        private void LoadSwitches()
        {
            var world = Singleton<GameWorld>.Instance.World;
            List<Switch> switches;
            if (world == null)
            {
                switches = FindObjectsOfType<Switch>().ToList();
            }
            else
            {
                switches = new List<Switch>();
                foreach (var worldInteractiveObject in world.WorldInteractiveObjects())
                {
                    var @switch = worldInteractiveObject.TryCast<Switch>();
                    if (@switch != null)
                    {
                        switches.Add(@switch);
                    }
                }
            }
            foreach (var @switch in switches)
            {
                if (!@switch.Operatable || !@switch.HasAuthority)
                {
                    continue;
                }

                if (@switch.ContextMenuTip.Contains("toggle_light"))
                {
                    continue;
                }
                var data = @switch.GetData();
                if (data != null)
                {
                    _datas.Add(data);
                }
            }
        }

        public SwitchData FindSwitch(string switchId)
        {
            if (string.IsNullOrEmpty(switchId))
            {
                return null;
            }

            foreach (SwitchData switchData in _datas)
            {
                if (switchData.Switch.Id == switchId)
                {
                    return switchData;
                }
            }
            return null;
        }
    }
}