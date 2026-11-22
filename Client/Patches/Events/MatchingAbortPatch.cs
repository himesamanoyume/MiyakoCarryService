using System.Reflection;
using EFT.UI.Matchmaker;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions.ToIl2Cpp;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 中断匹配时清空小队成员
    /// </summary>
    public sealed class MatchingAbortPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchmakerPlayersController), nameof(MatchmakerPlayersController.MatchingAbort));

        [PatchPrefix]
        public static void Prefix()
        {
            McsRequestHandler.ClearGroupMember().McsHandleExceptions();
        }
    }
}