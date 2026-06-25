

using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class DamageTriggerDataMgr : LabyrinthDataMgr<DamageTriggerDataMgr>
    {
        protected sealed override void OnRaidStarted()
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