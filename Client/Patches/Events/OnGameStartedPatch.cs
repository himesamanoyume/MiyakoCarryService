using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 处理GameWorld开始时的事件，同时进行护航生成
    /// </summary>
    public sealed class OnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        private static SubtitlesMgr SubtitlesMgr => MgrAccessor.Get<SubtitlesMgr>();

        [PatchPrefix]
        public static void Prefix()
        {
            if (McsMgr.IsHost)
            {
                TasksExtensions.HandleExceptions(GameLoop.Instance.SpawnMcsBotPlayer());
            }
        }

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