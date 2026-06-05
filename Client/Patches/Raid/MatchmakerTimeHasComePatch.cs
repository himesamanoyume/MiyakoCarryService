using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MiyakoCarryService.Client.Patches.Raid
{
    /// <summary>
    /// 使小队人数大于Scav人数限制时，直接在模式选择界面提示
    /// </summary>
    public sealed class MatchMakerSideSelectionScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertyGetter(typeof(MatchMakerSideSelectionScreen), nameof(MatchMakerSideSelectionScreen.Boolean_0));

        [PatchPrefix]
        public static bool Prefix(MatchmakerPlayerControllerClass ___MatchmakerPlayersController, ref bool __result)
        {
            if (___MatchmakerPlayersController?.GroupPlayers?.Count > MatchmakerPlayerControllerClass.MAX_SCAV_COUNT)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
