using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 仅让护航Bot执行额外的行为，并适配Fika
    /// </summary>
    public sealed class ManualUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotOwner), nameof(BotOwner.UpdateManual));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(BotOwner __instance)
        {
            if (__instance.GroupId is "Mcs" or "Fika" || McsMgr.IsMcsBotPlayer(__instance.ProfileId))
            {
                foreach(var botBehavior in __instance.GetBotBehaviors())
                {
                    botBehavior.ManualUpdate();
                }
            }
        }
    }
}