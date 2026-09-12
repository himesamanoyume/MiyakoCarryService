using System.Reflection;
using EFT.Vaulting;
using HarmonyLib;
using MiyakoCarryService.Client.Bots.Navigation;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 【2026-09-12 23:06 日志定死】"几何全达标却永远判 false"的最后一道门：交互距离的浮点边界。
    /// <para>
    /// VaultMoveModel.CheckVaultCondition:56 / ClimbMoveModel.CheckClimbCondition:73 都是
    /// <c>flag3 = MoveRestrictions.MinDistantToInteract >= distance</c>；而 distance 就是
    /// ObstacleCalculatorModel.DistanceToMainObstacle —— 它只是网格局部坐标的 z
    /// （CalculateDistance:195 直接 return p0.z，p0 是命中点经 world→local 逆变换的结果，
    /// 原始浮点、没有任何量化），站位正好落在第 5 格时实测是 0.5000000x，
    /// 而 globals.json 里 Vault 与 Climb 的 MinDistantToInteract 恰好是 0.5
    /// ⇒ <c>0.5 &gt;= 0.5000000x</c> 判负，两种翻越的 CanMove() 一起为 False。
    /// </para>
    /// <para>
    /// 实测 23:06:28.906：站位距边缘 0.48m、高 1.03 / 长 0.10 全在可翻区间，
    /// 背高比 / 背墙 / 顶净空 / 面墙全 √，唯独「距×≤0.50」（扫描值 距=0.50）；
    /// 两次尝试全废 → 拉黑 → 执行器放弃 ⇒ 0.06s 后虚拟点越过障碍、
    /// TryExtraSample 的 2m 兜底采样把 bot 一帧送到对面 1.1m（23:06:29.767 exec.castJump）。
    /// </para>
    /// <para>
    /// 修法：只在距离传进门之前把它抬到容差内，让"正好卡在阈值那一位"能通过 ——
    /// 不改门限本身、不减门数、不动其余六道门的输入。容差见 VaultGateProbe.InteractDistanceTolerance。
    /// 手动（TryVaulting → CanMove）与自动（DoVaultingTick → CanMoveAutomaticly）两条路用的是
    /// 两套不同的 MoveRestrictions（0.5 / 0.41），同一处缺陷，所以四个方法一起修，避免出现
    /// "手动过、自动不过"这种更难查的不一致。
    /// </para>
    /// </summary>
    public sealed class VaultInteractDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(VaultMoveModel), nameof(VaultMoveModel.CheckVaultCondition));

        [PatchPrefix]
        public static void Prefix(VaultMoveModel __instance, ref float distance)
        {
            VaultGateProbe.NudgeInteractDistance(__instance._settings?.MoveRestrictions, ref distance, nameof(VaultMoveModel.CheckVaultCondition));
        }
    }

    public sealed class VaultAutoInteractDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(VaultMoveModel), nameof(VaultMoveModel.CheckAutoVaultCondition));

        [PatchPrefix]
        public static void Prefix(VaultMoveModel __instance, ref float distance)
        {
            VaultGateProbe.NudgeInteractDistance(__instance._settings?.AutoMoveRestrictions, ref distance, nameof(VaultMoveModel.CheckAutoVaultCondition));
        }
    }

    public sealed class ClimbInteractDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClimbMoveModel), nameof(ClimbMoveModel.CheckClimbCondition));

        [PatchPrefix]
        public static void Prefix(ClimbMoveModel __instance, ref float distance)
        {
            VaultGateProbe.NudgeInteractDistance(__instance._settings?.MoveRestrictions, ref distance, nameof(ClimbMoveModel.CheckClimbCondition));
        }
    }

    public sealed class ClimbAutoInteractDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClimbMoveModel), nameof(ClimbMoveModel.CheckAutoClimbCondition));

        [PatchPrefix]
        public static void Prefix(ClimbMoveModel __instance, ref float distance)
        {
            VaultGateProbe.NudgeInteractDistance(__instance._settings?.AutoMoveRestrictions, ref distance, nameof(ClimbMoveModel.CheckAutoClimbCondition));
        }
    }
}
