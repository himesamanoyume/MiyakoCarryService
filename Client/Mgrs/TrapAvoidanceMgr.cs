
using System;
using System.Collections;
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public class TrapAvoidanceMgr : BaseMgr
    {
        private const float ENABLE_DIST = 8f;
        private const float DISABLE_DIST = 12f;
        private const float SCAN_INTERVAL = 0.1f;

        private readonly List<DelayedRecalc> _pendingRecalcs = new();

        private sealed class DelayedRecalc
        {
            public ObstacleData Trap;
            public BotOwner BotOwner;
            public float TriggerTime;
        }

        private McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();
        private TripwireDataMgr TripwireDataMgr => field ??= MgrAccessor.Get<TripwireDataMgr>();
        private RoomTrapDataMgr RoomTrapDataMgr => field ??= MgrAccessor.Get<RoomTrapDataMgr>();
        private DamageTriggerDataMgr DamageTriggerDataMgr => field ??= MgrAccessor.Get<DamageTriggerDataMgr>();
        private BarbedWireDataMgr BarbedWireDataMgr => field ??= MgrAccessor.Get<BarbedWireDataMgr>();
        private BorderZoneDataMgr BorderZoneDataMgr => field ??= MgrAccessor.Get<BorderZoneDataMgr>();

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (!Tools.IsHost)
            {
                return;
            }
            StartCoroutine(ScanLoop());
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            if (_pendingRecalcs != null)
            {
                _pendingRecalcs.Clear();
            }
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            if (_pendingRecalcs != null)
            {
                _pendingRecalcs.Clear();
            }
        }

        private IEnumerator ScanLoop()
        {
            var wait = new WaitForSeconds(SCAN_INTERVAL);
            while (true)
            {
                yield return wait;
                ScanAndApply();
                ProcessPendingRecalcs();
            }
        }

        private void ScanAndApply()
        {
            if (Gameloop == null || !Gameloop.IsVaildGameWorld)
            {
                return;
            }

            var mcsBotPlayers = McsMgr.GetAllAliveMcsBotPlayer();
            foreach (var trap in GetAllObstacles())
            {
                if (trap == null)
                {
                    continue;
                }

                if (!trap.IsActiveObstacle)
                {
                    trap.ApplyCarving(false);
                    continue;
                }

                var wasCarved = trap.IsCarvingApplied;
                var newCarve = ShouldCarve(trap, mcsBotPlayers);
                trap.ApplyCarving(newCarve);

                if (newCarve && !wasCarved)
                {
                    EnqueueRecalcNear(trap, mcsBotPlayers);
                }
            }
        }

        private void ProcessPendingRecalcs()
        {
            if (_pendingRecalcs.Count == 0)
            {
                return;
            }

            for (int i = _pendingRecalcs.Count - 1; i >= 0; i--)
            {
                var pending = _pendingRecalcs[i];
                if (pending.BotOwner == null || pending.Trap == null)
                {
                    _pendingRecalcs.RemoveAt(i);
                    continue;
                }

                _pendingRecalcs.RemoveAt(i);

                if (IsBotOwnerNearTrap(pending.BotOwner, pending.Trap))
                {
                    pending.BotOwner.Mover?.RecalcWay();
                }
            }
        }

        private void EnqueueRecalcNear(ObstacleData trap, List<Player> mcsBotPlayers)
        {
            if (mcsBotPlayers == null || mcsBotPlayers.Count == 0)
            {
                return;
            }

            var worldBounds = trap.GetObstacleWorldBounds();
            if (worldBounds == null || worldBounds.Count == 0)
            {
                return;
            }

            var time = Time.time;
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                if (mcsBotPlayer == null)
                {
                    continue;
                }

                var botOwner = mcsBotPlayer.AIData?.BotOwner;
                if (botOwner == null || botOwner.Mover == null)
                {
                    continue;
                }

                if (!IsPointNearBounds(mcsBotPlayer.Position, worldBounds, ENABLE_DIST))
                {
                    continue;
                }

                _pendingRecalcs.Add(new DelayedRecalc
                {
                    Trap = trap,
                    BotOwner = botOwner,
                    TriggerTime = time
                });
            }
        }

        private bool IsBotOwnerNearTrap(BotOwner botOwner, ObstacleData trap)
        {
            var worldBounds = trap.GetObstacleWorldBounds();
            if (worldBounds == null || worldBounds.Count == 0)
            {
                return false;
            }
            return IsPointNearBounds(botOwner.Position, worldBounds, ENABLE_DIST);
        }

        private static bool IsPointNearBounds(Vector3 point, List<Bounds> worldBounds, float dist)
        {
            foreach (var bounds in worldBounds)
            {
                var closest = bounds.ClosestPoint(point);
                if (Vector3.Distance(point, closest) < dist)
                {
                    return true;
                }
            }
            return false;
        }

        private bool ShouldCarve(ObstacleData trap, List<Player> mcsBotPlayers)
        {
            if (mcsBotPlayers == null || mcsBotPlayers.Count == 0)
            {
                return false;
            }

            var worldBounds = trap.GetObstacleWorldBounds();
            if (worldBounds == null || worldBounds.Count == 0)
            {
                return false;
            }

            var minDist = float.MaxValue;
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                if (mcsBotPlayer == null)
                {
                    continue;
                }

                var pos = mcsBotPlayer.Position;
                foreach (var bounds in worldBounds)
                {
                    var closest = bounds.ClosestPoint(pos);
                    var dist = Vector3.Distance(pos, closest);
                    if (dist < minDist)
                    {
                        minDist = dist;
                    }
                }
            }

            if (minDist < ENABLE_DIST)
            {
                return true;
            }
            if (minDist > DISABLE_DIST)
            {
                return false;
            }
            return trap.IsCarvingApplied;
        }

        private List<ObstacleData> GetAllObstacles()
        {
            var result = new List<ObstacleData>();

            foreach (var data in TripwireDataMgr.GetDatas<TripwireData>())
            {
                result.Add(data);
            }

            if (RoomTrapDataMgr.TrapDatas != null)
            {
                foreach (var trapDatas in RoomTrapDataMgr.TrapDatas.Values)
                {
                    foreach (var data in trapDatas)
                    {
                        result.Add(data);
                    }
                }
            }

            foreach (var data in DamageTriggerDataMgr.GetDatas<DamageTriggerData>())
            {
                result.Add(data);
            }

            foreach (var data in BarbedWireDataMgr.GetDatas<BarbedWireData>())
            {
                result.Add(data);
            }

            foreach (var data in BorderZoneDataMgr.GetDatas<BorderZoneData>())
            {
                result.Add(data);
            }

            return result;
        }
    }
}
