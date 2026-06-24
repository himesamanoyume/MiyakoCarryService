

using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class SwitchDataMgr : GameWorldDataMgr<SwitchDataMgr>
    {
        protected sealed override void OnRaidStarted()
        {
            LoadData(LoadSwitches);
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            OnRaidEnded();
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

                _datas.Add(@switch.GetData());
            }
        }
    }
}