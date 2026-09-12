using System.Text;
using EFT.Vaulting;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    // 翻越失败取证：TryVaulting() 只回一个 bool，而它内部 VaultMoveModel.CheckVaultCondition 有七道独立的门
    // （高度 / 厚度 / 距离 / 背后高度比 / 背后墙 / 头顶净空 / 面前墙），ClimbMoveModel 另有五道，
    // 任何一道不达标都表现为同一个 false → 光看返回值无法判断是哪一道。
    //
    // 这里把每道门两侧的量都读出来并逐项标注通过与否，一次定位卡在哪一道。
    // 数据源是组件自己的扫描器运行时输出（ObstacleCalculatorModel，Tick 时重算）与组件持有的 VaultingSettings，
    // 不复制阈值常量；子判定直接调 VaultMoveModel / ClimbMoveModel 的公开谓词方法，保证与真实判定同源
    public static class VaultGateProbe
    {
        public static string Describe(VaultingComponent component)
        {
            if (component == null || component._vaultingModelDebug == null)
            {
                return "VaultingComponent/模型为空";
            }

            var obstacle = component._vaultingModelDebug._obstacleCalculatorModel;
            var moves = component._vaultingModelDebug._vaultingStatesModel;
            var height = obstacle.MainObstacleHeight;
            var length = obstacle.MainObstacleLength;
            var distance = obstacle.DistanceToMainObstacle;

            var builder = new StringBuilder();
            builder.Append($"CanVaulting={component.CanVaulting()} strategy={component.GetVaultingStrategy()} IsVaulting={component.IsVaulting()}");
            builder.Append($" | IsActive={component._vaultingSettings.IsActive} ground={component._vaultingContext.IsGrounded}");
            builder.Append($" moveDirY={component._vaultingContext.MovementDirection.y:F2} sprint={component._vaultingContext.IsSprintEnabled} terrain={obstacle.IsTerrain}");
            builder.Append($" | 扫描值 高={height:F2} 长={length:F2} 距={distance:F2} 背高比={obstacle.BehindObstacleRatio:F2}");
            builder.Append($" 顶净空={(obstacle.MinRoofHeight.HasValue ? obstacle.MinRoofHeight.Value.ToString("F2") : "无")}");

            var vaultRestrictions = component._vaultingSettings.MovesSettings.VaultSettings.MoveRestrictions;
            if (vaultRestrictions != null)
            {
                builder.Append(" | Vault");
                builder.Append($" 高{Mark(height >= vaultRestrictions.MinHeight && height <= vaultRestrictions.MaxHeight)}[{vaultRestrictions.MinHeight:F2},{vaultRestrictions.MaxHeight:F2}]");
                builder.Append($" 长{Mark(length >= vaultRestrictions.MinLength && length <= vaultRestrictions.MaxLength)}[{vaultRestrictions.MinLength:F2},{vaultRestrictions.MaxLength:F2}]");
                builder.Append($" 距{Mark(vaultRestrictions.MinDistantToInteract >= distance)}≤{vaultRestrictions.MinDistantToInteract:F2}");
                builder.Append($" 背高比{Mark(moves._vaultState.CheckBehindObstacleHeightCondition())}");
                builder.Append($" 背墙{Mark(moves._vaultState.CheckBehindObstacleWall())}");
                builder.Append($" 顶{Mark(moves._vaultState.CheckRoofCondition())}");
                builder.Append($" 面墙{Mark(moves._vaultState.CheckBeforeObstacleWall())}");
                builder.Append($" → CanMove={moves._vaultState.CanMove()}");
            }

            var climbRestrictions = component._vaultingSettings.MovesSettings.ClimbSettings.MoveRestrictions;
            if (climbRestrictions != null)
            {
                builder.Append(" | Climb");
                builder.Append($" 高{Mark(height >= climbRestrictions.MinHeight && height <= climbRestrictions.MaxHeight)}[{climbRestrictions.MinHeight:F2},{climbRestrictions.MaxHeight:F2}]");
                builder.Append($" 长{Mark(length >= climbRestrictions.MinLength && length <= climbRestrictions.MaxLength)}[{climbRestrictions.MinLength:F2},{climbRestrictions.MaxLength:F2}]");
                builder.Append($" 距{Mark(climbRestrictions.MinDistantToInteract >= distance)}≤{climbRestrictions.MinDistantToInteract:F2}");
                builder.Append($" 顶{Mark(moves._climbState.CheckRoofCondition())}");
                builder.Append($" 面墙{Mark(moves._climbState.CheckBeforeObstacleWall())}");
                builder.Append($" → CanMove={moves._climbState.CanMove()}");
            }

            var target = obstacle.TargetCollider;
            builder.Append($" | 障碍={(target == null ? "null" : target.name)}");
            builder.Append(" | ");
            builder.Append(DescribeGrid(component));

            return builder.ToString();
        }

        // 组件扫描器在"一个可翻点都没扫到"时会留下退化值：WeightCalculatorModel.CalculateWeights
        // 在所有命中点都没通过高度限制时把 MaxWeightPoint 置 default(VaultingPoint)，而 default 的 Index
        // 恰好是 0，ObstacleCalculatorModel 里的 flag（Index < Count && Index >= 0）因此为真 —— 它便拿第 0 个
        // 竖直命中点（bot 脚下那个，通常正是地形）去算 IsTerrain，把"什么都没扫到"表达成"障碍是地形"，
        // CanVaulting() 于是直接 false、GetVaultingStrategy() 返回 None，七道门根本没参与判定。
        //
        // 反过来，只要 TargetCollider 非空就说明确实扫到了障碍，此时 DistanceToMainObstacle 才可信。
        // 判"贴到障碍跟前了没有"必须带上这个前提：退化时距离恒为 0，直接读会把 2m 开外误判成"已经贴上"
        public static bool TryMeasureObstacle(VaultingComponent component, out float distance)
        {
            distance = 0f;
            var obstacle = component?.VaultingModelDebug?.ObstacleCalculatorModelDebug;
            if (obstacle == null || obstacle.TargetCollider == null)
            {
                return false;
            }

            distance = obstacle.DistanceToMainObstacle;
            return true;
        }

        // 扫描网格原始状态。网格是"1 列 × 21 行"的竖直射线（GridSizeX = 0 ⇒ 只有 1 列；GridSizeZ = 2、
        // 步长 0.1 ⇒ 21 行），从脚下沿身体 yaw 铺到正前方 2m，步长 0.1m —— 障碍只有落在某一条射线上
        // 才会被记录。权重只让高度落在 [MinVaultingHeight, MaxVaultingHeight]（本机 0.25 ~ 1.31，取
        // Vault/Climb 四套 MoveRestrictions 的最小 MinHeight 与最大 MaxHeight）内的点参与。
        // 这里把网格自身的位置/朝向、命中点数、区间内的点数、最高点与最近命中体的图层一起打出来，
        // 用来区分"网格没覆盖到障碍"与"覆盖到了但高度不在可翻区间里"
        public static string DescribeGrid(VaultingComponent component)
        {
            if (component == null || component._vaultingModelDebug == null)
            {
                return "网格：组件为空";
            }

            var moves = component._vaultingModelDebug.VaultingStatesModel;
            var root = component._vaultingContext.VaultingGridRoot;
            var points = component._vaultingModelDebug.GridPointsModel.VerticalWorldHitPoints;

            var builder = new StringBuilder();
            builder.Append($"网格 root={(root == null ? "null" : root.position.ToString("F2"))}");
            if (root != null)
            {
                builder.Append($" rootFwd={root.forward.ToString("F2")}");
            }

            builder.Append($" 身体Fwd={component._vaultingContext.PlayerRealForward.ToString("F2")}");
            builder.Append($" 命中 {points.Count} 点");

            if (moves == null || points.Count == 0)
            {
                return builder.ToString();
            }

            builder.Append($"（可翻高度 [{moves.MinVaultingHeight:F2},{moves.MaxVaultingHeight:F2}]）");

            var highest = points[0];
            var inRange = 0;
            foreach (var point in points)
            {
                if (point.LocalPoint.y > highest.LocalPoint.y)
                {
                    highest = point;
                }

                if (point.LocalPoint.y >= moves.MinVaultingHeight && point.LocalPoint.y <= moves.MaxVaultingHeight)
                {
                    inRange++;
                }
            }

            builder.Append($" 区间内 {inRange} 点");
            builder.Append($" 最高 y={highest.LocalPoint.y:F2} z={highest.LocalPoint.z:F2}");
            builder.Append($" 最高体={DescribeCollider(highest.Collider)}");
            builder.Append($" 最近体={DescribeCollider(points[0].Collider)}");

            return builder.ToString();
        }

        private static string DescribeCollider(Collider collider)
        {
            return collider == null
                ? "null"
                : $"{collider.name}(L{LayerMask.LayerToName(collider.gameObject.layer)})";
        }

        private static string Mark(bool pass)
        {
            return pass ? "√" : "×";
        }
    }
}
