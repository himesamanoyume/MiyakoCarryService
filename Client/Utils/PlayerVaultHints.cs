using System.Collections.Generic;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    public static class PlayerVaultHints
    {
        private static readonly List<VaultHint> _hints = new List<VaultHint>();

        public static IReadOnlyList<VaultHint> All => _hints;

        public static void Add(VaultHint hint)
        {
            var start = hint.StartPos;
            start.y = 0f;
            for (var i = 0; i < _hints.Count; i++)
            {
                var existing = _hints[i].StartPos;
                existing.y = 0f;
                if (Vector3.Distance(start, existing) <= 1.5f)
                {
                    _hints[i] = hint;
                    return;
                }
            }

            _hints.Add(hint);
        }

        public static bool TryFind(Vector3 anchor, Vector3 dir, out VaultHint hint)
        {
            hint = null;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            var direction = dir.normalized;
            var best = float.MaxValue;
            foreach (var candidate in _hints)
            {
                var toStart = candidate.StartPos - anchor;
                toStart.y = 0f;
                var distance = toStart.magnitude;
                if (distance > 3f)
                {
                    continue;
                }

                var crossing = candidate.EndPos - candidate.StartPos;
                crossing.y = 0f;
                if (crossing.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                if (Vector3.Dot(crossing.normalized, direction) < 0.5f)
                {
                    continue;
                }

                if (distance < best)
                {
                    best = distance;
                    hint = candidate;
                }
            }

            return hint != null;
        }

        public static void Clear()
        {
            _hints.Clear();
        }
    }
}
