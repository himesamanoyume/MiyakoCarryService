
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 只有主机才会执行此函数，用于标识自身为主机
    /// </summary>
    public sealed class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsPresets), nameof(BotsPresets.TryLoadBotsProfilesOnStart));

        [PatchPostfix]
        public static async void Postfix(Task __result)
        {
            await __result;
            McsMgr.IsHost = true;
            TasksExtensions.HandleExceptions(GameLoop.Instance.SpawnMcsBotPlayer());
        }
    }
}