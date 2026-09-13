using System.Collections.Generic;
using EFT;
using EFT.Vaulting;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public class NavGapExecutor
    {
        private static readonly Dictionary<BotOwner, NavGapExecutor> _crossingBots = new Dictionary<BotOwner, NavGapExecutor>();
        private static readonly Dictionary<BotOwner, float> _recentHopEnds = new Dictionary<BotOwner, float>();
        private static readonly Dictionary<BotOwner, float> _recentAborts = new Dictionary<BotOwner, float>();
        private static readonly List<VaultBlacklistEntry> _vaultBlacklist = new List<VaultBlacklistEntry>();
        private static readonly Dictionary<BotOwner, RescueWatch> _rescueWatches = new Dictionary<BotOwner, RescueWatch>();

        private const float ABORT_GUARD_DURATION = 1.5f;
        private const float VAULT_BLACKLIST_RADIUS = 1.5f;
        private const float VAULT_BLACKLIST_DURATION = 60f;
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
            return botOwner != null && _crossingBots.TryGetValue(botOwner, out var executor) && executor._hopping;
        }

        public static bool IsApproaching(BotOwner botOwner)
        {
            if (botOwner == null)
            {
                return false;
            }

            if (_crossingBots.TryGetValue(botOwner, out var executor))
            {
                return !executor._hopping;
            }

            return _recentAborts.TryGetValue(botOwner, out var abortedAt) && Time.time - abortedAt < ABORT_GUARD_DURATION;
        }

        public static bool EndedHopRecently(BotOwner botOwner)
        {
            return botOwner != null && _recentHopEnds.TryGetValue(botOwner, out var time) && Time.time - time < 3f;
        }

        public static bool IsHoppingNear(Vector3 pos)
        {
            foreach (var executor in _crossingBots.Values)
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
            _vaultBlacklist.RemoveAll(e => e.Until > 0f && Time.time >= e.Until);
            foreach (var entry in _vaultBlacklist)
            {
                if (entry.Until > 0f && entry.Pos.McsSqrDistance(pos) <= VAULT_BLACKLIST_RADIUS * VAULT_BLACKLIST_RADIUS)
                {
                    return true;
                }
            }

            return false;
        }

        public static void BlacklistVault(Vector3 pos, bool immediate)
        {
            _vaultBlacklist.RemoveAll(e => e.Until > 0f && Time.time >= e.Until);
            for (var i = 0; i < _vaultBlacklist.Count; i++)
            {
                if (_vaultBlacklist[i].Pos.McsSqrDistance(pos) <= VAULT_BLACKLIST_RADIUS * VAULT_BLACKLIST_RADIUS)
                {
                    var entry = _vaultBlacklist[i];
                    entry.Fails++;
                    if (immediate || entry.Fails >= 3)
                    {
                        entry.Until = Time.time + VAULT_BLACKLIST_DURATION;
                    }

                    _vaultBlacklist[i] = entry;
                    return;
                }
            }

            _vaultBlacklist.Add(new VaultBlacklistEntry
            {
                Pos = pos,
                Until = immediate ? Time.time + VAULT_BLACKLIST_DURATION : 0f,
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
                _rescueWatches.Remove(botOwner);
                return;
            }

            if (!_rescueWatches.TryGetValue(botOwner, out var watch))
            {
                watch = new RescueWatch();
                _rescueWatches[botOwner] = watch;
            }

            watch.Update(botOwner, botOwner.Position, _crossingBots.ContainsKey(botOwner));

            if (_crossingBots.TryGetValue(botOwner, out var executor))
            {
                executor.Tick(botOwner);
            }
        }

        public void Begin(NavGapInfo gap, BotOwner botOwner)
        {
            var sameGap = _hasGap && _botOwner == botOwner && Vector3.Distance(gap.NearPoint, _lastGapNear) <= 1f;

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
            _crossingBots[botOwner] = this;
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

            if (!_hopping)
            {
                botOwner.GoToSomePointData.SetPoint(_edge);

                var toEdge = _edge - pos;
                toEdge.y = 0f;
                if (toEdge.magnitude <= 2.5f || botOwner.GoToSomePointData.IsCome())
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
                var obstacle = component?.VaultingModelDebug?.ObstacleCalculatorModelDebug;
                var hasObstacle = obstacle != null && obstacle.TargetCollider != null;

                if (toEdge.magnitude <= 2.5f)
                {
                    PressInto(botOwner);
                }
                else
                {
                    botOwner.GoToSomePointData.SetPoint(_edge);
                }

                if ((hasObstacle && obstacle.DistanceToMainObstacle <= 0.45f) || (toEdge.magnitude <= 1.2f && IsPressed(pos)))
                {
                    _arrived = true;
                    _arrivedTime = Time.time;
                    _nextVaultTryTime = Time.time + 0.2f;
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

            if (vaultingComponent != null
                && (vaultingComponent._vaultingContext.IsAnimatorInTransitionState(0) || vaultingComponent._vaultingContext.PlayerAnimatorIsJumpSetted())
                && Time.time - _arrivedTime < 2.5f)
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

        private void PressInto(BotOwner botOwner)
        {
            if (Time.time < _nextPressTime)
            {
                return;
            }

            _nextPressTime = Time.time + 0.25f;

            var direction = _destination - _edge;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
            }

            var pressTarget = _edge + direction;
            botOwner.GoToSomePointData.SetPoint(pressTarget);
            botOwner.Mover.GoToPointNoWay(pressTarget);
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
                    _recentHopEnds[_botOwner] = Time.time;
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
                _crossingBots.Remove(_botOwner);
            }
        }

        private void RecordAbort(BotOwner botOwner)
        {
            var now = Time.time;
            var stale = new List<BotOwner>();
            foreach (var pair in _recentAborts)
            {
                if (now - pair.Value >= ABORT_GUARD_DURATION)
                {
                    stale.Add(pair.Key);
                }
            }

            foreach (var owner in stale)
            {
                _recentAborts.Remove(owner);
            }

            _recentAborts[botOwner] = now;
        }
    }
}