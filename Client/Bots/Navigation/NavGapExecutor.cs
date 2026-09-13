using System.Collections.Generic;
using EFT;
using EFT.Vaulting;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public class NavGapExecutor
    {
        private static readonly Dictionary<BotOwner, NavGapExecutor> CrossingBots = new Dictionary<BotOwner, NavGapExecutor>();
        private static readonly Dictionary<BotOwner, float> RecentHopEnds = new Dictionary<BotOwner, float>();
        private static readonly Dictionary<BotOwner, float> RecentAborts = new Dictionary<BotOwner, float>();
        private const float AbortGuardDuration = 1.5f;
        private static readonly List<VaultBlacklistEntry> VaultBlacklist = new List<VaultBlacklistEntry>();
        private const float VaultBlacklistRadius = 1.5f;
        private const float VaultBlacklistDuration = 60f;
        private static readonly Dictionary<BotOwner, RescueWatch> RescueWatches = new Dictionary<BotOwner, RescueWatch>();

        private Vector3 _edge;
        private Vector3 _destination;
        private BotOwner _botOwner;
        private float _startTime;
        private bool _hopping;
        private ENavGapType _type;
        private bool _arrived;
        private int _vaultTryCount;
        private float _nextVaultTryTime;
        private float _hopStartTime;
        private float _arrivedTime;
        private Vector3 _lastGapNear;
        private bool _hasGap;
        private Vector3 _pressAnchor;
        private float _pressAnchorTime;
        private float _nextPressTime;

        public bool IsCrossing { get; private set; }

        public float NextPlanCooldownUntil { get; private set; }

        public static bool RelinkInProgress;

        public static bool RescueInProgress;

        public bool IsHoppingNow => _hopping;

        public static bool IsHopping(BotOwner botOwner)
        {
            return botOwner != null && CrossingBots.TryGetValue(botOwner, out var executor) && executor._hopping;
        }

        public static bool IsApproaching(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return false;
            }

            if (CrossingBots.TryGetValue(botOwner, out var executor))
            {
                return !executor._hopping;
            }

            return RecentAborts.TryGetValue(botOwner, out var abortedAt) && Time.time - abortedAt < AbortGuardDuration;
        }

        public static bool EndedHopRecently(BotOwner botOwner, float within)
        {
            return botOwner != null && RecentHopEnds.TryGetValue(botOwner, out var time) && Time.time - time < within;
        }

        public static bool IsHoppingNear(Vector3 pos)
        {
            foreach (var executor in CrossingBots.Values)
            {
                if (executor._hopping && executor._botOwner != null && executor._botOwner.Position.McsSqrDistance(pos) <= 1f)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsVaultBlacklisted(Vector3 pos)
        {
            VaultBlacklist.RemoveAll(e => e.Until > 0f && Time.time >= e.Until);
            foreach (var entry in VaultBlacklist)
            {
                if (entry.Until > 0f && entry.Pos.McsSqrDistance(pos) <= VaultBlacklistRadius * VaultBlacklistRadius)
                {
                    return true;
                }
            }

            return false;
        }

        public static void BlacklistVault(Vector3 pos, bool immediate)
        {
            VaultBlacklist.RemoveAll(e => e.Until > 0f && Time.time >= e.Until);
            for (var i = 0; i < VaultBlacklist.Count; i++)
            {
                if (VaultBlacklist[i].Pos.McsSqrDistance(pos) <= VaultBlacklistRadius * VaultBlacklistRadius)
                {
                    var entry = VaultBlacklist[i];
                    entry.Fails++;
                    if (immediate || entry.Fails >= 3)
                    {
                        entry.Until = Time.time + VaultBlacklistDuration;
                    }
                    VaultBlacklist[i] = entry;
                    return;
                }
            }

            VaultBlacklist.Add(new VaultBlacklistEntry
            {
                Pos = pos,
                Until = immediate ? Time.time + VaultBlacklistDuration : 0f,
                Fails = 1,
            });
        }

        public static void OnMoverTick(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return;
            }

            if (botOwner.GetPlayer == null)
            {
                RescueWatches.Remove(botOwner);
                return;
            }

            WatchRescue(botOwner, botOwner.Position);

            if (CrossingBots.TryGetValue(botOwner, out var executor))
            {
                executor.Tick(botOwner);
            }
        }

        private static void WatchRescue(BotOwner botOwner, Vector3 position)
        {
            if (!RescueWatches.TryGetValue(botOwner, out var watch))
            {
                watch = new RescueWatch();
                RescueWatches[botOwner] = watch;
            }

            var now = Time.time;
            if (now >= watch.NextProbeTime)
            {
                watch.NextProbeTime = now + 0.5f;
                if (NavMesh.SamplePosition(position, out var sample, 0.5f, -1))
                {
                    watch.LastGoodPos = sample.position;
                    watch.HasLastGood = true;
                    watch.Anchor = position;
                    watch.AnchorTime = now;
                    return;
                }
            }

            if (CrossingBots.ContainsKey(botOwner) || !watch.HasLastGood)
            {
                return;
            }

            if (position.McsSqrDistance(watch.Anchor) > 0.5f * 0.5f)
            {
                watch.Anchor = position;
                watch.AnchorTime = now;
                return;
            }

            if (now - watch.AnchorTime < 3f)
            {
                return;
            }

            watch.Anchor = watch.LastGoodPos;
            watch.AnchorTime = now;
            watch.NextProbeTime = now + 1f;
            RescueInProgress = true;
            try
            {
                botOwner.Mover.SetPlayerToNavMesh(watch.LastGoodPos);
            }
            finally
            {
                RescueInProgress = false;
            }
        }

        public void Begin(NavGapInfo gap, BotOwner botOwner)
        {
            var sameGap = _hasGap && _botOwner == botOwner
                && Vector3.Distance(gap.NearPoint, _lastGapNear) <= 1f;

            _lastGapNear = gap.NearPoint;
            _hasGap = true;
            _edge = gap.NearPoint;
            _destination = gap.FarPoint;
            _type = gap.Type;
            _botOwner = botOwner;
            _startTime = Time.time;
            _hopping = false;
            _arrived = false;
            if (!sameGap)
            {
                _vaultTryCount = 0;
            }

            _nextVaultTryTime = 0f;
            _pressAnchor = botOwner.Position;
            _pressAnchorTime = Time.time;
            _arrivedTime = 0f;
            _nextPressTime = 0f;
            IsCrossing = true;
            CrossingBots[botOwner] = this;
        }

        private void Tick(BotOwner botOwner)
        {
            var pos = botOwner.Position;

            if (_type == ENavGapType.Vault)
            {
                TickVault(botOwner, pos);
                return;
            }

            if (ShouldFinish(botOwner))
            {
                FinishAndRelink(botOwner);
                return;
            }

            var botPosition = botOwner.Position;
            if (!_hopping)
            {
                botOwner.GoToSomePointData.SetPoint(_edge);

                var toEdgeNow = _edge - botPosition;
                toEdgeNow.y = 0f;
                if (toEdgeNow.magnitude <= 2.5f || botOwner.GoToSomePointData.IsCome())
                {
                    _hopping = true;
                    botOwner.GoToSomePointData.Point = _destination;
                }
                return;
            }

            botOwner.Mover.GoToPointNoWay(_destination);
            botOwner.GoToSomePointData.Point = _destination;
            botOwner.GoToSomePointData._lastPosibleRecalc = Time.time;
            botOwner.GoToSomePointData._pointhRefreshed = false;
        }

        private void TickVault(BotOwner botOwner, Vector3 pos)
        {
            if (_hopping)
            {
                if (IsAcrossObstacle(pos))
                {
                    FinishAndRelink(botOwner);
                    return;
                }

                if (Time.time - _hopStartTime > 2.5f && IsPressed(pos) && !NavMesh.SamplePosition(pos, out _, 0.5f, -1))
                {
                    BlacklistVault(_edge, true);
                    Cancel();
                    return;
                }

                if (Time.time - _startTime > 12f)
                {
                    BlacklistVault(_edge, true);
                    Cancel();
                }
                return;
            }

            if (IsAcrossObstacle(pos))
            {
                FinishAndRelink(botOwner);
                return;
            }

            if (!_arrived)
            {
                var toEdge = _edge - pos;
                toEdge.y = 0f;

                var component = botOwner.GetPlayer.VaultingComponent as VaultingComponent;
                component?.Tick();
                var hasObstacle = VaultGateProbe.TryMeasureObstacle(component, out var obstacleDistance);

                if (toEdge.magnitude <= 2.5f)
                {
                    PressInto(botOwner);
                }
                else
                {
                    botOwner.GoToSomePointData.SetPoint(_edge);
                }

                if (hasObstacle && obstacleDistance <= 0.45f)
                {
                    MarkArrived();
                    return;
                }

                if (toEdge.magnitude <= 1.2f && IsPressed(pos))
                {
                    MarkArrived();
                    return;
                }

                if (Time.time - _startTime > 8f)
                {
                    NextPlanCooldownUntil = Time.time + 8f;
                    Cancel();
                }
                return;
            }

            var faceDir = _destination - _edge;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.001f)
            {
                faceDir.Normalize();
                botOwner.Steering.LookToPoint(_edge + faceDir * 2f);
            }

            if (botOwner.GetPlayer.MovementContext.IsSprintEnabled)
            {
                botOwner.Mover.Sprint(false);
            }

            var vaultingComponent = botOwner.GetPlayer.VaultingComponent as VaultingComponent;

            if (Time.time < _nextVaultTryTime)
            {
                PressInto(botOwner);
                return;
            }

            if (VaultGateProbe.IsAnimatorBusy(vaultingComponent) && Time.time - _arrivedTime < 2.5f)
            {
                PressInto(botOwner);
                return;
            }

            VaultAim.FaceYaw(botOwner, faceDir);
            vaultingComponent?.Tick();

            if (!VaultAim.TryAlign(botOwner, vaultingComponent, faceDir))
            {
                NextPlanCooldownUntil = Time.time + 8f;
                Cancel();
                return;
            }

            _vaultTryCount++;
            if (botOwner.GetPlayer.VaultingComponent.TryVaulting())
            {
                botOwner.Mover.Stop();
                botOwner.GetPlayer.OnVaulting();
                _hopping = true;
                _hopStartTime = Time.time;
                return;
            }

            if (_vaultTryCount >= 2)
            {
                BlacklistVault(_edge, true);
                Cancel();
            }
            else
            {
                _nextVaultTryTime = Time.time + 0.8f;
            }
        }

        private void MarkArrived()
        {
            _arrived = true;
            _arrivedTime = Time.time;
            _nextVaultTryTime = Time.time + 0.2f;
        }

        private void PressInto(BotOwner botOwner)
        {
            if (Time.time < _nextPressTime)
            {
                return;
            }

            _nextPressTime = Time.time + 0.25f;
            var pressTarget = PressTarget();
            botOwner.GoToSomePointData.SetPoint(pressTarget);
            botOwner.Mover.GoToPointNoWay(pressTarget);
        }

        private Vector3 PressTarget()
        {
            var dir = _destination - _edge;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
            }

            return _edge + dir;
        }

        private bool IsPressed(Vector3 pos)
        {
            if (pos.McsSqrDistance(_pressAnchor) > 0.05f * 0.05f)
            {
                _pressAnchor = pos;
                _pressAnchorTime = Time.time;
                return false;
            }

            return Time.time - _pressAnchorTime > 0.4f;
        }

        private bool IsAcrossObstacle(Vector3 pos)
        {
            var dir = _destination - _edge;
            dir.y = 0f;
            var total = dir.magnitude;
            if (total < 0.01f)
            {
                return false;
            }

            return Vector3.Dot(pos - _edge, dir / total) > total * 0.6f && NavMesh.SamplePosition(pos, out _, 0.3f, -1);
        }

        private void FinishAndRelink(BotOwner botOwner)
        {
            Cancel();
            RelinkInProgress = true;
            try
            {
                botOwner.Mover.SetPlayerToNavMesh(botOwner.Position);
            }
            finally
            {
                RelinkInProgress = false;
            }
        }

        private bool ShouldFinish(BotOwner botOwner)
        {
            var pos = botOwner.Position;
            if (pos.McsSqrDistance(_destination) <= 1f * 1f)
            {
                return NavMesh.SamplePosition(pos, out _, 0.3f, -1);
            }

            if (_hopping && pos.y - _destination.y <= 0.2f)
            {
                return NavMesh.SamplePosition(pos, out _, 0.3f, -1);
            }

            if (Time.time - _startTime > 10f)
            {
                NextPlanCooldownUntil = Time.time + 8f;
                return true;
            }

            return false;
        }

        public void Cancel()
        {
            if (_botOwner != null)
            {
                if (_hopping)
                {
                    RecentHopEnds[_botOwner] = Time.time;
                }
                else
                {
                    _botOwner.Mover.Stop();
                    RecordAbort(_botOwner);
                }
            }

            IsCrossing = false;
            _hopping = false;
            _arrived = false;
            if (_botOwner != null)
            {
                CrossingBots.Remove(_botOwner);
            }
        }

        private static void RecordAbort(BotOwner botOwner)
        {
            var now = Time.time;
            if (RecentAborts.Count >= 8)
            {
                var stale = new List<BotOwner>();
                foreach (var pair in RecentAborts)
                {
                    if (now - pair.Value >= AbortGuardDuration)
                    {
                        stale.Add(pair.Key);
                    }
                }

                foreach (var owner in stale)
                {
                    RecentAborts.Remove(owner);
                }
            }

            RecentAborts[botOwner] = now;
        }
    }
}