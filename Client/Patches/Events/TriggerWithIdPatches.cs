using System;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 让护航也能触发老板的TriggerEnter检测
    /// </summary>
    public sealed class TriggerWithIdEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TriggerWithId), nameof(TriggerWithId.TriggerEnter));

        [PatchPostfix]
        public static void Postfix(TriggerWithId __instance, Player player)
        {
            TriggerPatchShared.HandleTriggerEnter(__instance, player);
        }
    }

    /// <summary>
    /// 5.0 专属补丁：ExperienceTrigger 覆写了 TriggerEnter，而 IL2CPP 把基类 TriggerWithId.TriggerEnter 的方法体
    /// 内联进了这个覆写体（反编译 ExperienceTrigger::TriggerEnter 可见 TriggerZones 的 Contains/Add 序列），
    /// 于是虚调用只会进入覆写体，打在基类上的 TriggerWithIdEnterPatch 对查勘点型触发器（*_exploration）永不触发。
    /// 这里单独补一个覆写补丁，处理逻辑与基类补丁共用（两者不会重复：覆写体里没有对基类的真实调用）。
    /// </summary>
    public sealed class ExperienceTriggerEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ExperienceTrigger), nameof(ExperienceTrigger.TriggerEnter));

        [PatchPostfix]
        public static void Postfix(ExperienceTrigger __instance, Player player)
        {
            TriggerPatchShared.HandleTriggerEnter(__instance, player);
        }
    }

    /// <summary>
    /// 让护航也能触发老板的TriggerExit检测
    /// </summary>
    public sealed class TriggerWithIdExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TriggerWithId), nameof(TriggerWithId.TriggerExit));

        [PatchPostfix]
        public static void Postfix(TriggerWithId __instance, Player player)
        {
            TriggerPatchShared.HandleTriggerExit(__instance, player);
        }
    }

    /// <summary>
    /// 触发器进/出补丁的共用实现。
    /// </summary>
    internal static class TriggerPatchShared
    {
        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        public static void HandleTriggerEnter(TriggerWithId trigger, Player player)
        {
            // 包一层兜底：postfix 里逃逸的异常会被 IL2CPP trampoline 吞掉并返回垃圾值，只留错误日志更安全。
            try
            {
                if (player == null)
                {
                    return;
                }

                if (!McsMgr.IsMcsMemberPlayer(player.ProfileId, out var mcsLeadPlayer) || mcsLeadPlayer == null)
                {
                    return;
                }

                mcsLeadPlayer.AddTriggerZone(trigger);

                // 5.0：__instance 的声明类型是基类 TriggerWithId，托管 `is ExperienceTrigger` 恒为 false → TryCast
                var experienceTrigger = trigger == null ? null : trigger.TryCast<ExperienceTrigger>();
                if (experienceTrigger != null)
                {
                    mcsLeadPlayer.SpecialPlaceVisited(experienceTrigger.Id, experienceTrigger._experience);
                }
            }
            catch (Exception e)
            {
                McsLogger.LogError(e);
            }
        }

        public static void HandleTriggerExit(TriggerWithId trigger, Player player)
        {
            try
            {
                if (player == null)
                {
                    return;
                }

                if (!McsMgr.IsMcsMemberPlayer(player.ProfileId, out var mcsLeadPlayer) || mcsLeadPlayer == null)
                {
                    return;
                }

                mcsLeadPlayer.RemoveTriggerZone(trigger);
            }
            catch (Exception e)
            {
                McsLogger.LogError(e);
            }
        }
    }
}
