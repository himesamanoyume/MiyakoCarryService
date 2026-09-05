using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    public sealed class SainPlayVoiceLinePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(SAINUtils.PlayerComponentType, "PlayVoiceLine");

        private static SubtitlesMgr SubtitlesMgr => field ??= MgrAccessor.Get<SubtitlesMgr>();

        [PatchPrefix]
        public static bool Prefix(object __instance, EPhraseTrigger phrase, ETagStatus mask, bool aggressive)
        {
            var botOwner = SAINUtils.GetPlayerComponentBotOwner(__instance);
            var player = botOwner?.GetPlayer;
            if (player == null)
            {
                return true;
            }

            if (SubtitlesMgr.ShouldSilenceBot(player))
            {
                return false;
            }

            return true;
        }
    }
}