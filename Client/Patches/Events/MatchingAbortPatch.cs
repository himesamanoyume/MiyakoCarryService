using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 中断匹配时清空小队成员
    /// </summary>
    public sealed class MatchingAbortPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerPlayerControllerClass), nameof(MatchmakerPlayerControllerClass.MatchingAbort));

        [PatchPrefix]
        public static void Prefix()
        {
            TasksExtensions.HandleExceptions(McsRequestHandler.ClearGroupMember());
        }
    }
}