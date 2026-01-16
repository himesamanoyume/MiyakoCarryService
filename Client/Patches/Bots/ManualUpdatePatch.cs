using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    internal sealed class ManualUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotOwner), nameof(BotOwner.UpdateManual));

        [PatchPostfix]
        public static void Postfix(BotOwner __instance)
        {
            if (__instance.GroupId == "mcs" || __instance.GroupId == "fika")
            {
                foreach(var botBehavior in __instance.GetBotBehaviors())
                {
                    botBehavior.ManualUpdate();
                }
            }
        }
    }
}