using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    public static class NavGapDetector
    {
        // 翻越检测参数（硬编码）
        private const float VaultObstacleMaxDistance = 15f;   // 障碍搜索距离
        private const float VaultMaxThickness = 2.5f;         // 障碍最大厚度
        private const float VaultStandOffset = 0.4f;          // 站立点取在障碍面前多远（EFT 只在距障碍 ≤0.5m 时才给翻越策略）

        // 连通场景（路径本来就能走通）下，翻越必须省下这么多路程才值得做。
        // 判据是"省下来的绝对路程"：绕路 20m、翻越后 3m ⇒ 省 17m 该翻；绕路 20m、翻越后 19m ⇒ 只省 1m 不该翻
        private const float VaultMinSaving = 2f;

        public static bool TryDetectGap(Vector3 startPos, Vector3 targetPos, NavMeshPath path, out NavGapInfo gap)
        {
            // 翻越（窗户/围栏）优先于走下：可翻障碍通常比可跨越落差更近
            if (TryDetectVault(startPos, targetPos, path, out gap))
            {
                return true;
            }

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

        // 翻越检测：沿 bot→目标方向射线找障碍，粗过滤（目标在另一侧/对侧有同层 navmesh/厚度/非黑名单），
        // 成败细节交给 VaultingComponent 到场自判（TryVaulting 内部自带完整障碍扫描与 Vault/Climb 策略）
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

            // 多高度水平扫描：围栏/矮墙常见高度 0.8~1.2m，而 EFT 的可翻上限也才 1.1m(Vault)/1.31m(Climb)，
            // 单条"胸口高"（+1.2m）的射线会整个从矮围栏头顶掠过 —— 实测 17:42 日志里 noHit 与
            // behindTarget 投影为负（-1.39~-7.58）同时出现，正是"射线飞过围栏、打到目标后方更远的结构"
            if (!TryRaycastObstacle(startPos, dir, out var hit))
            {
                NavBridgeDebug.Log("vault.plan.noHit", $"拒绝：{VaultObstacleMaxDistance:F0}m 内沿 bot→目标方向（{dir.ToString("F2")}）0.5~1.4m 多高度扫描未命中障碍（pos={startPos.ToString("F1")} target={targetPos.ToString("F1")} 目标水平距 {targetDistance:F1}m，绕路 {PathLength(path.corners):F1}m / {path.status}，面前 {DescribeNearby(startPos, dir)}）");
                return false;
            }

            var face = hit.point;

            // 目标必须在障碍另一侧，否则翻过去反而背向目标。
            // 投影 = 目标水平距 - 命中距：为负说明射线已越过目标，多半是没扫到真正的围栏
            var behindProjection = Vector3.Dot(targetPos - face, dir);
            if (behindProjection <= 0.3f)
            {
                NavBridgeDebug.Log("vault.plan.behindTarget", $"拒绝：目标不在障碍另一侧（投影 {behindProjection:F2}，命中距 {hit.distance:F2}m vs 目标水平距 {targetDistance:F2}m，命中 {hit.collider.name} 于 {face.ToString("F1")}）");
                return false;
            }

            // 黑名单：近期翻越失败的障碍（60s 内直接走寻路结果，不再撞墙）
            if (NavGapExecutor.IsVaultBlacklisted(face))
            {
                NavBridgeDebug.Log("vault.plan.blacklisted", $"拒绝：障碍在翻越黑名单（距 {hit.distance:F2}m face={face.ToString("F1")}）");
                return false;
            }

            // 【2026-09-13 日志定死】障碍高度判定。原先的探针是 face - dir * 0.25f（退到障碍"外侧"）
            // 从 3m 高垂直下打 —— 打中的是脚前的地面（topHit.y - startPos.y ≈ 0），永远不拒绝，
            // 全日志 vault.plan.tooHigh 只出现 2 次。后果是卡车 kamaz_5490_COLLIDER（顶面约 3.5m）、
            // 轿车 Subaru_Legacy_Closed_COLLIDER 都被规划翻越 —— 共 19 次车辆，日志里"命中面高 0.50m"
            // 全是 0.5m 那条射线先命中车侧造成的假象。
            // 改成"跨越柱占用检测"：在障碍面内侧 0.25m、可翻上限之上取一点测有没有实体。
            // 可翻上限是 Vault 1.10m / Climb 1.31m（MoveRestrictions），所以 1.35m 处还有实体就必然翻不过去：
            //   · 轿车（车顶约 1.4m）/ 卡车（约 3.5m）→ 该处是实心 ⇒ 拒绝
            //   · 围栏、混凝土路障（1.0~1.25m）→ 该处已在障碍之上 ⇒ 放行
            //   · 窗户（窗洞约 1.0~1.7m）→ 面内侧 0.25m 正好是窗洞 ⇒ 放行
            // 这里必须用体积检测 CheckSphere 而不是向下 Raycast：从上方打射线时，障碍越高起点越容易
            // 落在碰撞体内部，而 Unity 的 Raycast 不检测"起点所在"的碰撞体 ⇒ 卡车那种高障碍反而测不到
            var crossingColumn = face + dir * 0.25f;
            crossingColumn.y = startPos.y;
            if (Physics.CheckSphere(crossingColumn + Vector3.up * 1.35f, 0.1f, LayersMaskController.PlayerStaticCollisionsMask))
            {
                NavBridgeDebug.Log("vault.plan.tooHigh", $"拒绝：跨越柱 1.35m 高处是实体（可翻上限 Vault 1.10 / Climb 1.31m），障碍顶面高于可翻高度（命中面高 {face.y - startPos.y:F2}m face={face.ToString("F1")}）");
                return false;
            }

            // 厚度 + 对侧落点：从障碍面起沿方向逐段前移并垂直下探，找到低于障碍面的对侧地面
            Vector3 farPoint = default;
            var found = false;
            for (var offset = 0.4f; offset <= VaultMaxThickness; offset += 0.3f)
            {
                var probe = face + dir * offset + Vector3.up * 0.6f;
                if (!Physics.Raycast(probe, Vector3.down, out var downHit, 6f, LayersMaskController.PlayerStaticCollisionsMask))
                {
                    continue;
                }

                // 探针仍落在障碍顶面/近侧结构上（近侧地面 = startPos.y，高出 0.2m 以上就不是"对面地面"）
                if (downHit.point.y > startPos.y + 0.2f)
                {
                    continue;
                }

                // 对侧落点必须是 navmesh、与起点同层（窗户/围栏两侧同层）、且确实在障碍另一侧
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
                NavBridgeDebug.Log("vault.plan.noFarPoint", $"拒绝：障碍另一侧 {VaultMaxThickness:F1}m 内找不到同层 navmesh 落点（face={face.ToString("F1")}）");
                return false;
            }

            // 站立点：障碍面前 VaultStandOffset 处取 navmesh 样点，且 bot 到该点必须有完整路径。
            // 早前把站立点写成"越过障碍面 0.3m"（face + dir * 0.3）：face 是胸口高（+1.2m）的命中点，
            // 那个点既在障碍内部、又悬在 bot 头顶的高度 → bot 根本走不过去 → 接近阶段永远到不了 →
            // 钉在原地不动 → 运行期前瞻探测也永远扫不到障碍（17:10 日志：plan.ok 之后 11s 位置零变化、
            // 没有任何 vault.exec.try，全是 check.noHit）。站立点必须是真实可站的 navmesh 点
            var standQuery = face - dir * VaultStandOffset;
            if (!NavMesh.SamplePosition(standQuery, out var standSample, 0.75f, -1))
            {
                NavBridgeDebug.Log("vault.plan.noNearMesh", $"拒绝：障碍前站立点附近没有 navmesh（query={standQuery.ToString("F1")}）");
                return false;
            }

            var toNearPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(startPos, standSample.position, -1, toNearPath) || toNearPath.status != NavMeshPathStatus.PathComplete)
            {
                NavBridgeDebug.Log("vault.plan.nearUnreachable", $"拒绝：bot→站立点无完整路径（sample={standSample.position.ToString("F1")} status={toNearPath.status}）");
                return false;
            }

            // EFT 只在"距障碍 ≤0.5m"时才给翻越策略
            // （VaultMoveModel.CheckVaultCondition 的 flag3 = MoveRestrictions.MinDistantToInteract >= DistanceToMainObstacle）
            var standPoint = standSample.position;
            var standDistance = Vector3.Dot(face - standPoint, dir);

            // 【2026-09-13 日志定死】这里的"节省量"原先量纲是错的：拿"整条绕路的长度"去减"到站立点 + 2m"。
            // path.corners 是 bot→移动目标的完整路线（实测目标常在 90m 外），而 toNearLength + 2f 只是走到
            // 障碍脚下那几米 —— 实测差恒为正且巨大（60.8 / 112.0 / 81.1m），"仍选择翻越"永远成立，
            // 于是 96%（171/178）的翻越规划都发生在路径本来就能走通（PathComplete）的时候，
            // 护航舍近求远跑去翻一个根本不挡路的障碍（用户报的"过度想翻越"）。
            // 正确的比较是"翻越之后的剩余路程"：到站立点 + 跨过障碍 + 从落点继续走到目标。
            // 这样绕路 20m 而翻越后只要 3m 会判"该翻"，绕路 20m 而翻越后还要 19m 则判"不该翻"
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                var toNearLength = PathLength(toNearPath.corners);
                var detourLength = PathLength(path.corners);
                var fromFarPath = new NavMeshPath();
                if (!NavMesh.CalculatePath(farPoint, targetPos, -1, fromFarPath))
                {
                    NavBridgeDebug.Log("vault.plan.noSaving", $"拒绝：落点→目标算不出路径（落点={farPoint.ToString("F1")} target={targetPos.ToString("F1")}），无法证明翻越更省，不翻");
                    return false;
                }

                // 跨越障碍本身按 2.0m 计：站立点顶到障碍面 + 翻到对面落点
                var fromFarLength = PathLength(fromFarPath.corners);
                var vaultLength = toNearLength + 2f + fromFarLength;
                var saving = detourLength - vaultLength;
                if (saving < VaultMinSaving)
                {
                    NavBridgeDebug.Log("vault.plan.noSaving", $"拒绝：翻越省不下路程（绕路 {detourLength:F1}m vs 翻越 {vaultLength:F1}m = 到站立点 {toNearLength:F1} + 跨越 2.0 + 落点到目标 {fromFarLength:F1}，只省 {saving:F1}m < {VaultMinSaving:F1}m）");
                    return false;
                }

                NavBridgeDebug.Log("vault.plan.saving", $"连通场景：绕路 {detourLength:F1}m，翻越 {vaultLength:F1}m（省 {saving:F1}m ≥ {VaultMinSaving:F1}m），选择翻越");
            }

            NavBridgeDebug.Log("vault.plan.ok", $"规划翻越：障碍 {hit.collider.name} 距 {Vector3.Distance(face, startPos):F2}m（命中面高 {face.y - startPos.y:F2}m），站位={standPoint.ToString("F1")}（距障碍面 {standDistance:F2}m），落点={farPoint.ToString("F1")}");
            gap = new NavGapInfo
            {
                Type = ENavGapType.Vault,
                NearPoint = standPoint,
                FarPoint = farPoint,
                Way = BuildWay(toNearPath.corners, farPoint),
            };
            return true;
        }

        // 多高度水平扫描：从膝高到略高于胸口逐层打水平射线，只认垂直面（过滤地形），取沿 dir 最近的一个。
        // 单条射线不够用：围栏/矮墙常在 0.8~1.2m，而 EFT 的可翻上限也只有 1.1m(Vault)/1.31m(Climb)，
        // 从胸口（+1.2m）平打会整个从矮围栏头顶掠过 —— 于是日志里出现"扫不到障碍"或"打到目标后方更远的结构"
        private static bool TryRaycastObstacle(Vector3 startPos, Vector3 dir, out RaycastHit hit)
        {
            hit = default;
            var found = false;
            var nearest = float.MaxValue;
            for (var height = 0.5f; height <= 1.4f; height += 0.3f)
            {
                if (!Physics.Raycast(startPos + Vector3.up * height, dir, out var candidate, VaultObstacleMaxDistance, LayersMaskController.PlayerStaticCollisionsMask)
                    || candidate.collider == null)
                {
                    continue;
                }

                // 命中面接近水平的是地面/坡面，不是可翻障碍
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

        // 诊断：多高度扫描落空时，把面前 1.5m 内的碰撞体直接列出来（名字/图层/碰撞体高度），
        // 用来区分"前方确实没障碍"与"障碍存在但扫描没覆盖到（高度不够、或图层不在静态碰撞掩码里）"
        private static string DescribeNearby(Vector3 startPos, Vector3 dir)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var collider in Physics.OverlapSphere(startPos + dir * 1.5f + Vector3.up * 0.8f, 1.5f))
            {
                if (collider == null)
                {
                    continue;
                }

                builder.Append(builder.Length > 0 ? " / " : string.Empty);
                builder.Append($"{collider.name}(L{LayerMask.LayerToName(collider.gameObject.layer)} h{collider.bounds.size.y:F1})");
                if (builder.Length > 160)
                {
                    break;
                }
            }
            return builder.Length == 0 ? "无" : builder.ToString();
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

            // bot 脚下不是 navmesh（行进循环的第一个采样点就断开 ⇒ edgeDist=0、edge 退化成 startPos 自身）时，
            // 「bot 能走到边缘」这道门必须跳过：CalculatePath 的起点就不在网格上，结果必然不是 PathComplete，
            // 于是唯一可行的方案被它自己拒掉，bot 永远留在上面。实测形态是 EFT 的 Climb 策略把护航抬到
            // 护栏顶上，顶上没有 navmesh ⇒ 功能③一直不触发，最后只能靠 EFT 的救援传送瞬移下来。
            // 脚下没有网格时不存在「走到边缘」这回事：bot 已经站在边缘上了，路径就是原地一条直线
            Vector3[] toEdgeCorners;
            float toEdgeLen;
            if (edgeDist <= 0.01f)
            {
                toEdgeCorners = new[] { startPos };
                toEdgeLen = 0f;
                NavBridgeDebug.Log("march.edgeFromAir", $"起点不在 navmesh 上（edge 退化为 bot 自身位置 {startPos.ToString("F1")}），跳过走到边缘的路径校验，直接按原地下跨规划：落差 {drop:F2}m，落点 {targetPos.ToString("F1")}");
            }
            else
            {
                // bot 必须能简洁地走到边缘（直线被墙隔断时拒绝）
                var toEdgePath = new NavMeshPath();
                if (!NavMesh.CalculatePath(startPos, edge, -1, toEdgePath) || toEdgePath.status != NavMeshPathStatus.PathComplete)
                {
                    //NavBridgeDebug.Log("march.toEdgePath", "拒绝：bot→边缘 无完整路径（直线被隔断）");
                    return false;
                }

                toEdgeLen = PathLength(toEdgePath.corners);
                if (toEdgeLen > edgeDist + 2.5f)
                {
                    //NavBridgeDebug.Log("march.toEdgeLen", $"拒绝：到边缘路径 {toEdgeLen:F1}m 远超直线 {edgeDist:F1}m");
                    return false;
                }

                toEdgeCorners = toEdgePath.corners;
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
            gap = BuildGap(edge, targetPos, toEdgeCorners, toEdgeCorners.Length);
            return true;
        }

        private static NavGapInfo BuildGap(Vector3 nearPoint, Vector3 farPoint, Vector3[] corners, int count)
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
