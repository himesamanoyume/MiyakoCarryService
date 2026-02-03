using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 仅让护航Bot执行额外的行为，并适配Fika
    /// </summary>
    // internal sealed class ManualUpdatePatch : ModulePatch
    // {
    //     protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotOwner), nameof(BotOwner.UpdateManual));

    //     [PatchPostfix]
    //     public static void Postfix(BotOwner __instance)
    //     {
    //         if (__instance.GroupId == "mcs" || __instance.GroupId == "fika")
    //         {
    //             // foreach(var botBehavior in __instance.GetBotBehaviors())
    //             // {
    //             //     botBehavior.ManualUpdate();
    //             // }
    //         }
    //     }
    // }
}