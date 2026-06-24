

using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;

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

                if (Classification.SwitchBlacklistTips.Contains(@switch.ContextMenuTip))
                {
                    continue;
                }

                _datas.Add(@switch.GetData());
            }
        }
    }
}