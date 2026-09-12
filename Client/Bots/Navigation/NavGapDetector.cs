using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public static class NavGapDetector
    {
        public static bool TryDetectGap(Vector3 startPos, Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                return TryDetectStepDownOnPartial(targetPos, path, out gap);
            }

            if (path.status == NavMeshPathStatus.PathInvalid)
            {
                // 落差点 Unity 有时直接返回 Invalid 而非 Partial：bot 已无路可走，必选跨越
                return TryDetectStepDownOnDetour(startPos, targetPos, path, out gap);
            }

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                return TryDetectStepDownOnDetour(startPos, targetPos, path, out gap);
            }

            //NavBridgeDebug.Log("detect.status", $"跳过：status={path.status}");
            gap = null;
            return false;
        }

        public static float PathLength(Vector3[] corners)
        {
            var total = 0f;
            for (var i = 0; i < corners.Length - 1; i++)
            {
                total += Vector3.Distance(corners[i], corners[i + 1]);
            }
            return total;
        }

        // 断开型：路径只能延伸到缺口边缘；落点 = 玩家所在的下层 navmesh 位置（与玩家水平位置解耦，玩家只需在下层）
        private static bool TryDetectStepDownOnPartial(Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            gap = null;
            if (path.corners.Length == 0)
            {
                //NavBridgeDebug.Log("partial.corners", "拒绝：corners 为空");
                return false;
            }

            var edge = path.corners[path.corners.Length - 1];

            // 玩家必须在边缘下方的下层区域（水平 15m 内），但不要求在边缘正上方
            var edgeToTarget = targetPos - edge;
            edgeToTarget.y = 0f;
            if (targetPos.y >= edge.y - 0.25f || edgeToTarget.magnitude > 15f)
            {
                //NavBridgeDebug.Log("partial.playerNotBelow", $"拒绝：玩家不在边缘下层（落差 {edge.y - targetPos.y:F2}m，水平 {edgeToTarget.magnitude:F1}m）");
                return false;
            }

            // 落差过小视为同层，超过上限不可直接走下
            var drop = edge.y - targetPos.y;
            if (drop > 5f)
            {
                //NavBridgeDebug.Log("partial.dropTooHigh", $"拒绝：落差 {drop:F2}m > 5");
                return false;
            }

            // 落点直接取玩家位置：TryProjectToGround 已保证其在下层网格上，
            // 不能用 SamplePosition 在边缘下方找落点——低落差时上层面距查询点更近，采样必然命中上层面
            //NavBridgeDebug.Log("partial.ok", $"规划成功：edge={edge.ToString("F1")} landing={targetPos.ToString("F1")} drop={drop:F1} 玩家水平 {edgeToTarget.magnitude:F1}m");
            gap = BuildGap(edge, targetPos, path.corners, path.corners.Length);
            return true;
        }

        // 行进式搜索：沿 bot→目标方向在上层网格上行进，踏空处即边缘。
        // 适用于 Complete（连通但绕路，需节省对比）与 Invalid（无路可走，直接规划）
        private static bool TryDetectStepDownOnDetour(Vector3 startPos, Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            gap = null;
            var complete = path.status == NavMeshPathStatus.PathComplete;

            // 目标不在起点下方时没有"走下"可言
            if (targetPos.y >= startPos.y - 0.25f)
            {
                //NavBridgeDebug.Log("march.notBelow", $"跳过：目标不在起点下方 start.y={startPos.y:F2} target.y={targetPos.y:F2}");
                return false;
            }

            var dir = targetPos - startPos;
            dir.y = 0f;
            if (dir.magnitude < 0.5f)
            {
                //NavBridgeDebug.Log("march.dirShort", "拒绝：水平方向过短");
                return false;
            }
            dir.Normalize();

            // 沿直线在上层网格上行进，采样失败或层高骤降处即为边缘
            var edge = startPos;
            var edgeDist = 0f;
            for (var d = 0.5f; d <= 15f; d += 0.5f)
            {
                var q = startPos + dir * d;
                q.y = startPos.y;
                if (!NavMesh.SamplePosition(q, out var sample, 1f, -1) || sample.position.y < startPos.y - 0.25f)
                {
                    //NavBridgeDebug.Log("march.broke", $"行进 {d:F1}m 处踏空/出网格");
                    break;
                }
                edge = sample.position;
                edgeDist = d;
            }

            // 边缘必须位于 bot 与目标之间（否则走下反而远离目标）
            var edgeToTarget = targetPos - edge;
            edgeToTarget.y = 0f;
            if (edgeToTarget.magnitude > 8f)
            {
                //NavBridgeDebug.Log("march.edgeToTarget", $"拒绝：边缘距目标水平 {edgeToTarget.magnitude:F1}m > 8，edge={edge.ToString("F1")}");
                return false;
            }

            var drop = edge.y - targetPos.y;
            // 落差过小视为同层，超过上限不可直接走下
            if (drop < 0.25f || drop > 5f)
            {
                //NavBridgeDebug.Log("march.dropRange", $"拒绝：落差 {drop:F2}m 超出 [0.25, 5]");
                return false;
            }

            // bot 必须能简洁地走到边缘（直线被墙隔断时拒绝）
            var toEdgePath = new NavMeshPath();
            if (!NavMesh.CalculatePath(startPos, edge, -1, toEdgePath) || toEdgePath.status != NavMeshPathStatus.PathComplete)
            {
                //NavBridgeDebug.Log("march.toEdgePath", "拒绝：bot→边缘 无完整路径（直线被隔断）");
                return false;
            }

            var toEdgeLen = PathLength(toEdgePath.corners);
            if (toEdgeLen > edgeDist + 2.5f)
            {
                //NavBridgeDebug.Log("march.toEdgeLen", $"拒绝：到边缘路径 {toEdgeLen:F1}m 远超直线 {edgeDist:F1}m");
                return false;
            }

            // 连通场景需要证明捷径确实更省；Invalid 场景无路可走，跨就是了
            if (complete)
            {
                var total = PathLength(path.corners);
                if (total - (toEdgeLen + drop) < 2f)
                {
                    //NavBridgeDebug.Log("march.saving", $"拒绝：节省不足 2m（绕路 {total:F1}m vs 捷径 {toEdgeLen + drop:F1}m）");
                    return false;
                }
            }
            else
            {
                //NavBridgeDebug.Log("march.invalid", "路径 Invalid（无路可走），直接规划跨越");
            }

            //NavBridgeDebug.Log("march.ok", $"规划成功：edge={edge.ToString("F1")} landing={targetPos.ToString("F1")} drop={drop:F1} toEdge={toEdgeLen:F1}m");
            gap = BuildGap(edge, targetPos, toEdgePath.corners, toEdgePath.corners.Length);
            return true;
        }

        private static NavGapInfo BuildGap(Vector3 nearPoint, Vector3 farPoint, Vector3[] corners, int count)
        {
            var way = new Vector3[count + 1];
            System.Array.Copy(corners, way, count);
            way[^1] = farPoint;

            return new NavGapInfo
            {
                Type = ENavGapType.StepDown,
                NearPoint = nearPoint,
                FarPoint = farPoint,
                Way = way,
            };
        }
    }
}
