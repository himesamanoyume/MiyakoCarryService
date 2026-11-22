
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EFT.GameTriggers;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using System;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class RoomTrapDataMgr : LabyrinthDataMgr
    {
        public RoomTrapDataMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public ConcurrentDictionary<ELabyrinthTrapType, HashSet<TriggerZoneData>> TrapDatas;

        public override void OnRaidStarted()
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
            StartCoroutine(ReloadDataLoop(2f, RefreshTrapState).McsSafe());
        }

        public override void OnRaidEnded()
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
                var triggerZoneId = triggerZone._triggerId;
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

        public bool IsTrapDisabled(int labyrinthTrapType)
        {
            var trapType = (ELabyrinthTrapType)labyrinthTrapType;
            if (TrapDatas == null)
            {
                return false;
            }
            var triggerZoneDatas = TrapDatas.GetOrAdd(trapType, _ => new());
            return !triggerZoneDatas.All(triggerZoneData => TriggersEmitter.Instance._ignoreTriggers.Contains(triggerZoneData.Id));
        }

        private void RefreshTrapState()
        {
            if (TrapDatas == null)
            {
                return;
            }

            foreach ((var labyrinthTrapType, var triggerZoneDatas) in TrapDatas)
            {
                var isTrapDisabled = IsTrapDisabled((int)labyrinthTrapType);
                foreach (var triggerZoneData in triggerZoneDatas)
                {
                    triggerZoneData.SetObstacle(isTrapDisabled);
                }
            }
        }
    }
}