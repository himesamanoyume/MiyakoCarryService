
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EFT.GameTriggers;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public class RoomTrapDataMgr : LabyrinthDataMgr<RoomTrapDataMgr>
    {
        public ConcurrentDictionary<ELabyrinthTrapType, HashSet<TriggerZoneData>> TrapDatas;

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (!Tools.IsHost)
            {
                return;
            }
            if (!_shouldInit)
            {
                return;
            }
            TrapDatas = new();
            LoadData(LoadRoomTrap);
            StartCoroutine(ReloadDataLoop(2f, RefreshTrapState));
        }

        protected sealed override void OnRaidEnded()
        {
            base.OnRaidEnded();
            if (TrapDatas != null)
            {
                TrapDatas.Clear();
            }
            TrapDatas = null;
        }

        private void LoadRoomTrap()
        {
            if (TrapDatas == null)
            {
                TrapDatas = new();
            }

            var triggerZones = FindObjectsOfType<TriggerZone>();
            foreach (var triggerZone in triggerZones)
            {
                var triggerZoneId = (string)AccessTools.Field(typeof(TriggerZone), "_triggerId").GetValue(triggerZone);
                foreach ((var labyrinthTrapType, var ids) in Classification.LabyrinthTrapIds)
                {
                    if (!ids.Contains(triggerZoneId))
                    {
                        continue;
                    }

                    var data = triggerZone.GetData(triggerZoneId);
                    if (data != null)
                    {
                        var triggerZoneDatas = TrapDatas.GetOrAdd(labyrinthTrapType, _ => new());
                        triggerZoneDatas.Add(data);
                    }
                }
            }
        }

        public bool IsTrapDisabled(ELabyrinthTrapType labyrinthTrapType)
        {
            if (TrapDatas == null)
            {
                return false;
            }
            var triggerZoneDatas = TrapDatas.GetOrAdd(labyrinthTrapType, _ => new());
            return triggerZoneDatas.All(triggerZoneData => GClass3592.Instance.HashSet_0.Contains(triggerZoneData.Id));
        }

        private void RefreshTrapState()
        {
            if (TrapDatas == null)
            {
                return;
            }


        } 
    }
}