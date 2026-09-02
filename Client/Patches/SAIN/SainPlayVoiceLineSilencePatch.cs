

using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    public sealed class SainPlayVoiceLineSilencePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(Type.GetType("SAIN.Components.PlayerComponent, SAIN"), "PlayVoiceLine", [typeof(EPhraseTrigger), typeof(ETagStatus), typeof(bool)]);

        private static SubtitlesMgr SubtitlesMgr => field ??= MgrAccessor.Get<SubtitlesMgr>();

        [PatchPrefix]
        public static bool Prefix(Player ___Player)
        {
            if (SubtitlesMgr.ShouldSilenceBot(___Player))
            {
                return false;
            }

            return true;
        }
    }
}
