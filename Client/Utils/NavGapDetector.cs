using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Bots.Navigation;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Utils
{
    public static class NavGapDetector
    {
        public static bool TryDetectGap(Vector3 startPos, Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            if (TryDetectPlayerHint(startPos, targetPos, path, out gap))
            {
                return true;
            }

            if (TryDetectStepUp(startPos, targetPos, path, out gap))
            {
                return true;
            }

            if (TryDetectVault(startPos, targetPos, path, out gap))
            {
                return true;
            }

            if (path.status == NavMeshPathStatus.PathPartial)
            {
                return TryDetectStepDownOnPartial(targetPos, path, out gap);
            }

            if (path.status is NavMeshPathStatus.PathInvalid or NavMeshPathStatus.PathComplete)
            {
                return TryDetectStepDownOnDetour(startPos, targetPos, path, out gap);
            }

            gap = null;
            return false;
        }

        private static bool TryDetectPlayerHint(Vector3 startPos, Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            gap = null;
            var toTarget = targetPos - startPos;
            toTarget.y = 0f;
            var targetDistance = toTarget.magnitude;
            if (targetDistance < 1f)
            {
                return false;
            }

            var dir = toTarget / targetDistance;
            if (!PlayerVaultHints.TryFind(startPos, dir, out var hint, 10f))
            {
                return false;
            }

            if (NavGapExecutor.IsVaultBlacklisted(hint.StartPos))
            {
                return false;
            }

            if (!NavMesh.SamplePosition(hint.StartPos, out var nearSample, 0.75f, -1) || !NavMesh.SamplePosition(hint.EndPos, out var farSample, 1f, -1))
            {
                return false;
            }

            if (Vector3.Dot(farSample.position - startPos, dir) <= Vector3.Dot(nearSample.position - startPos, dir))
            {
                return false;
            }

            var toNearPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(startPos, nearSample.position, -1, toNearPath) || toNearPath.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                var fromFarPath = new NavMeshPath();
                if (!NavMesh.CalculatePath(farSample.position, targetPos, -1, fromFarPath))
                {
                    return false;
                }

                var hintLength = PathLength(toNearPath.corners) + (farSample.position - nearSample.position).magnitude + PathLength(fromFarPath.corners);
                if (PathLength(path.corners) - hintLength < 2f)
                {
                    return false;
                }
            }

            gap = new NavGapInfo
            {
                Type = ENavGapType.Vault,
                NearPoint = nearSample.position,
                FarPoint = farSample.position,
                Way = BuildWay(toNearPath.corners, farSample.position),
            };
            return true;
        }

        private static bool TryDetectStepUp(Vector3 startPos, Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            gap = null;
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            var toTarget = targetPos - startPos;
            toTarget.y = 0f;
            var targetDistance = toTarget.magnitude;
            if (targetDistance < 1f)
            {
                return false;
            }

            var dir = toTarget / targetDistance;
            if (!TryRaycastObstacle(startPos, dir, out var hit))
            {
                return false;
            }

            var face = hit.point;
            if (Vector3.Dot(targetPos - face, dir) <= 0.3f)
            {
                return false;
            }

            if (NavGapExecutor.IsVaultBlacklisted(face))
            {
                return false;
            }

            var climbMaxHeight = Singleton<GlobalConfiguration>.Instance.VaultingSettings.MovesSettings.ClimbSettings.MoveRestrictions.MaxHeight;
            if (climbMaxHeight <= 0f)
            {
                return false;
            }

            var crossingColumn = face + dir * 0.25f;
            crossingColumn.y = startPos.y;
            if (Physics.CheckSphere(crossingColumn + Vector3.up * 1.35f, 0.1f, LayersMaskController.PlayerStaticCollisionsMask))
            {
                return false;
            }

            Vector3 farPoint = default;
            var found = false;
            for (var offset = 0.4f; offset <= 2.5f; offset += 0.3f)
            {
                var probe = face + dir * offset + Vector3.up * 2f;
                if (!Physics.Raycast(probe, Vector3.down, out var downHit, 8f, LayersMaskController.PlayerStaticCollisionsMask))
                {
                    continue;
                }

                var stepHeight = downHit.point.y - startPos.y;
                if (stepHeight < 0.6f || stepHeight > climbMaxHeight)
                {
                    continue;
                }

                if (NavMesh.SamplePosition(downHit.point, out var farSample, 1f, -1)
                    && farSample.position.y - startPos.y >= 0.6f
                    && farSample.position.y - startPos.y <= climbMaxHeight
                    && Vector3.Dot(farSample.position - face, dir) > 0.3f)
                {
                    farPoint = farSample.position;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            var standQuery = face - dir * 0.4f;
            if (!NavMesh.SamplePosition(standQuery, out var standSample, 0.75f, -1))
            {
                return false;
            }

            var toNearPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(startPos, standSample.position, -1, toNearPath) || toNearPath.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            gap = new NavGapInfo
            {
                Type = ENavGapType.StepUp,
                NearPoint = standSample.position,
                FarPoint = farPoint,
                Way = BuildWay(toNearPath.corners, farPoint),
            };
            return true;
        }

        private static bool TryDetectVault(Vector3 startPos, Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            gap = null;
            var toTarget = targetPos - startPos;
            toTarget.y = 0f;
            var targetDistance = toTarget.magnitude;
            if (targetDistance < 1f)
            {
                return false;
            }

            var dir = toTarget / targetDistance;
            if (!TryRaycastObstacle(startPos, dir, out var hit))
            {
                return false;
            }

            var face = hit.point;
            if (Vector3.Dot(targetPos - face, dir) <= 0.3f)
            {
                return false;
            }

            if (NavGapExecutor.IsVaultBlacklisted(face))
            {
                return false;
            }

            var crossingColumn = face + dir * 0.25f;
            crossingColumn.y = startPos.y;
            if (Physics.CheckSphere(crossingColumn + Vector3.up * 1.35f, 0.1f, LayersMaskController.PlayerStaticCollisionsMask))
            {
                return false;
            }

            Vector3 farPoint = default;
            var found = false;
            for (var offset = 0.4f; offset <= 2.5f; offset += 0.3f)
            {
                var probe = face + dir * offset + Vector3.up * 0.6f;
                if (!Physics.Raycast(probe, Vector3.down, out var downHit, 6f, LayersMaskController.PlayerStaticCollisionsMask))
                {
                    continue;
                }

                if (downHit.point.y > startPos.y + 0.2f)
                {
                    continue;
                }

                if (NavMesh.SamplePosition(downHit.point, out var farSample, 1.5f, -1)
                    && Mathf.Abs(farSample.position.y - startPos.y) < 0.6f
                    && Vector3.Dot(farSample.position - face, dir) > 0.3f)
                {
                    farPoint = farSample.position;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            var standQuery = face - dir * 0.4f;
            if (!NavMesh.SamplePosition(standQuery, out var standSample, 0.75f, -1))
            {
                return false;
            }

            var toNearPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(startPos, standSample.position, -1, toNearPath) || toNearPath.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                var fromFarPath = new NavMeshPath();
                if (!NavMesh.CalculatePath(farPoint, targetPos, -1, fromFarPath))
                {
                    return false;
                }

                var vaultLength = PathLength(toNearPath.corners) + 2f + PathLength(fromFarPath.corners);
                if (PathLength(path.corners) - vaultLength < 2f)
                {
                    return false;
                }
            }

            gap = new NavGapInfo
            {
                Type = ENavGapType.Vault,
                NearPoint = standSample.position,
                FarPoint = farPoint,
                Way = BuildWay(toNearPath.corners, farPoint),
            };
            return true;
        }

        private static bool TryRaycastObstacle(Vector3 startPos, Vector3 dir, out RaycastHit hit)
        {
            hit = default;
            var found = false;
            var nearest = float.MaxValue;
            for (var height = 0.5f; height <= 1.4f; height += 0.3f)
            {
                if (!Physics.Raycast(startPos + Vector3.up * height, dir, out var candidate, 15f, LayersMaskController.PlayerStaticCollisionsMask)
                    || candidate.collider == null)
                {
                    continue;
                }

                if (candidate.normal.y > 0.7f)
                {
                    continue;
                }

                if (candidate.distance < nearest)
                {
                    nearest = candidate.distance;
                    hit = candidate;
                    found = true;
                }
            }

            return found;
        }

        private static float PathLength(Vector3[] corners)
        {
            var total = 0f;
            for (var i = 0; i < corners.Length - 1; i++)
            {
                total += Vector3.Distance(corners[i], corners[i + 1]);
            }

            return total;
        }

        private static bool TryDetectStepDownOnPartial(Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            gap = null;
            if (path.corners.Length == 0)
            {
                return false;
            }

            var edge = path.corners[path.corners.Length - 1];
            var edgeToTarget = targetPos - edge;
            edgeToTarget.y = 0f;
            if (targetPos.y >= edge.y - 0.25f || edgeToTarget.magnitude > 15f)
            {
                return false;
            }

            var drop = edge.y - targetPos.y;
            if (drop > 5f)
            {
                return false;
            }

            gap = BuildGap(edge, targetPos, path.corners);
            return true;
        }

        private static bool TryDetectStepDownOnDetour(Vector3 startPos, Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            gap = null;
            var complete = path.status == NavMeshPathStatus.PathComplete;
            if (targetPos.y >= startPos.y - 0.25f)
            {
                return false;
            }

            var dir = targetPos - startPos;
            dir.y = 0f;
            if (dir.magnitude < 0.5f)
            {
                return false;
            }

            dir.Normalize();
            var edge = startPos;
            var edgeDist = 0f;
            for (var d = 0.5f; d <= 15f; d += 0.5f)
            {
                var q = startPos + dir * d;
                q.y = startPos.y;
                if (!NavMesh.SamplePosition(q, out var sample, 1f, -1) || sample.position.y < startPos.y - 0.25f)
                {
                    break;
                }

                edge = sample.position;
                edgeDist = d;
            }

            var edgeToTarget = targetPos - edge;
            edgeToTarget.y = 0f;
            if (edgeToTarget.magnitude > 8f)
            {
                return false;
            }

            var drop = edge.y - targetPos.y;
            if (drop < 0.25f || drop > 5f)
            {
                return false;
            }

            Vector3[] toEdgeCorners;
            float toEdgeLen;
            if (edgeDist <= 0.01f)
            {
                toEdgeCorners = new[] { startPos };
                toEdgeLen = 0f;
            }
            else
            {
                var toEdgePath = new NavMeshPath();
                if (!NavMesh.CalculatePath(startPos, edge, -1, toEdgePath) || toEdgePath.status != NavMeshPathStatus.PathComplete)
                {
                    return false;
                }

                toEdgeLen = PathLength(toEdgePath.corners);
                if (toEdgeLen > edgeDist + 2.5f)
                {
                    return false;
                }

                toEdgeCorners = toEdgePath.corners;
            }

            if (complete && PathLength(path.corners) - (toEdgeLen + drop) < 2f)
            {
                return false;
            }

            gap = BuildGap(edge, targetPos, toEdgeCorners);
            return true;
        }

        private static NavGapInfo BuildGap(Vector3 nearPoint, Vector3 farPoint, Vector3[] corners)
        {
            return new NavGapInfo
            {
                Type = ENavGapType.StepDown,
                NearPoint = nearPoint,
                FarPoint = farPoint,
                Way = BuildWay(corners, farPoint),
            };
        }

        private static Vector3[] BuildWay(Vector3[] corners, Vector3 farPoint)
        {
            var way = new Vector3[corners.Length + 1];
            System.Array.Copy(corners, way, corners.Length);
            way[^1] = farPoint;
            return way;
        }
    }
}
