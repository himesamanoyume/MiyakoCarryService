using EFT;
using EFT.Vaulting;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Navigation
{
    // 翻越前的身体朝向对齐。
    //
    // EFT 的翻越扫描沿身体 yaw，而不是视线或移动方向：VaultingComponent.Tick 把
    // MovementContext.PlayerRealForward（= Quaternion.Euler(0, Rotation.x, 0) * Vector3.forward）塞进
    // VaultingTransferModel，GridPointsModel 就按这个轴铺前向网格。身体朝哪边，扫出来的就是哪边。
    //
    // 而 BotSteering.LookToPoint 只是把转向模式设成 ToCustomPoint 并记下目标点（BotSteering.cs:100），
    // 真正的旋转在 Steering.ManualFixedUpdate → Steering() 里按速率逐帧推进；护航跟随中
    // GoToPointLogic.Update 每帧调 LookToMovingDirection()（GoToPointLogic.cs:29），会把转向目标顶回移动方向。
    // 于是"LookToPoint 之后同帧调 TryVaulting"扫到的还是旧朝向，策略必然是 None。
    //
    // 这里按 BotSteering.SetXAngle 的写法直接写身体 yaw（Player.Rotate(new Vector2(deltaYaw, 0f), true)），
    // 返回本次修正的角度（度），供日志区分"本来就朝着障碍"和"不得不掰过来"
    public static class VaultAim
    {
        public static float FaceYaw(BotOwner botOwner, Vector3 direction)
        {
            var player = botOwner.GetPlayer;
            direction.y = 0f;
            if (player == null || direction.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            var deltaYaw = Mathf.DeltaAngle(player.Rotation.x, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
            if (Mathf.Abs(deltaYaw) < 1f)
            {
                return 0f;
            }

            player.Rotate(new Vector2(deltaYaw, 0f), true);
            return Mathf.Abs(deltaYaw);
        }

        // 朝向自检：写正身体 yaw 并不等于"网格就扫在障碍上"。实测同一障碍 "修正 5° 就成功、119° 就失败"，
        // 失败时扫描退化成"本地全地面"（长=2.00、地形=true），七道门一次都没参与。两处会脱节：
        //
        // ① player.Rotate 最终落到当前移动状态的 Rotate 上，而 MovementContext.Rotation 的 setter 还会再过一次
        //    ClampRotation（按 _yawLimit 夹紧）。大角度修正可能被夹掉一截，身体其实没转到位；
        // ② 网格 root 是骨架上的骨骼，跟随时机与 MovementContext.Rotation 不一定同帧。
        //
        // 两者任一脱节，紧跟其后的 TryVaulting 都注定扫不到障碍，只白吃一次失败额度（还可能拉黑该障碍）。
        // 所以这里回读实测夹角：超限就再掰一次朝向 + 补 Tick；仍超限返回 false，由调用方放弃本次尝试。
        // detail 无论通过与否都带实测角度，方便直接进日志定位（阈值 15° 只用一次，内联）
        public static bool TryAlign(BotOwner botOwner, VaultingComponent component, Vector3 direction, out string detail)
        {
            var bodyOff = MeasureBodyOffAngle(botOwner, direction);
            var gridOff = MeasureGridOffAngle(component);
            if (bodyOff <= 15f && gridOff <= 15f)
            {
                detail = $"朝向自检 身体{bodyOff:F0}°/网格{gridOff:F0}°";
                return true;
            }

            var retryAngle = FaceYaw(botOwner, direction);
            component?.Tick();
            var retryBodyOff = MeasureBodyOffAngle(botOwner, direction);
            var retryGridOff = MeasureGridOffAngle(component);

            detail = $"朝向自检未过 身体{bodyOff:F0}°/网格{gridOff:F0}° → 再掰 {retryAngle:F0}° 后 身体{retryBodyOff:F0}°/网格{retryGridOff:F0}°";
            return retryBodyOff <= 15f && retryGridOff <= 15f;
        }

        // 身体朝向与目标方向的夹角。PlayerRealForward 正是网格铺设用的轴
        // （MovementContext.method_10 里由 Rotation.x 算出，缓存在 _playerRealForward）
        private static float MeasureBodyOffAngle(BotOwner botOwner, Vector3 direction)
        {
            var player = botOwner.GetPlayer;
            direction.y = 0f;
            if (player == null || direction.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            var bodyForward = player.MovementContext.PlayerRealForward;
            bodyForward.y = 0f;
            return Vector3.Angle(bodyForward, direction);
        }

        // 网格自身朝向与身体朝向的夹角：两者不一致说明扫描扫的不是身体所指的方向（骨骼跟随有延迟）
        private static float MeasureGridOffAngle(VaultingComponent component)
        {
            var gridRoot = component?._vaultingContext?.VaultingGridRoot;
            if (gridRoot == null)
            {
                return 0f;
            }

            var gridForward = gridRoot.forward;
            gridForward.y = 0f;
            var bodyForward = component._vaultingContext.PlayerRealForward;
            bodyForward.y = 0f;
            return Vector3.Angle(gridForward, bodyForward);
        }
    }
}
