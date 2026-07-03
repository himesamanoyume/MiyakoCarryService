

using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public class SwitchDataMgr : GameWorldDataMgr
    {
        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadSwitches);
        }

        private void LoadSwitches()
        {
            var switches = Singleton<GameWorld>.Instance.World_0.WorldInteractiveObjects().OfType<Switch>();
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