using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.GameWorldEvent
{
    /// <summary>
    /// 处理战局开始时的事件
    /// </summary>
    internal sealed class GameWorldOnGameStartedPatch : ModulePatch
    {
        internal static Action OnGameWorldStarted;
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));

        [PatchPostfix]
        public static void Postfix()
        {
            GameLoop.Instance.IsGameStarted = true;
            GameLoop.Instance.CheckVaildGameWorld();
            OnGameWorldStarted?.Invoke();
        }
    }
}