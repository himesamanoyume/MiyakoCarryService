using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 处理GameWorld开始时的事件
    /// </summary>
    internal sealed class OnGameStartedPatch : ModulePatch
    {
        internal static Action OnGameWorldStart;
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));

        [PatchPostfix]
        public static void Postfix()
        {
            GameLoop.Instance.CheckVaildGameWorld();
            OnGameWorldStart?.Invoke();
        }
    }
}