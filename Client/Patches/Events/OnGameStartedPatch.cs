using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 处理GameWorld开始时的事件
    /// </summary>
    public sealed class OnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));

        [PatchPostfix]
        public static void Postfix()
        {
            GameLoop.Instance.IsGameStarted = true;
            GameLoop.Instance.CheckVaildGameWorld();
            EventMgr.Notify(new GameWorldStartedEvent
            {
                GameWorld = Singleton<GameWorld>.Instance,
            });
        }
    }
}