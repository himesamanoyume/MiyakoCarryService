

using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public class DamageTriggerDataMgr : LabyrinthDataMgr
    {
        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (!Tools.IsHost)
            {
                return;
            }
            LoadData(LoadDamageTrigger);
        }

        private void LoadDamageTrigger()
        {
            var damageTriggers = FindObjectsOfType<DamageTrigger>();
            foreach (var damageTrigger in damageTriggers)
            {
                var data = damageTrigger.GetData();
                if (data != null)
                {
                    _datas.Add(data);
                }
            }
        }
    }
}