
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 自己实现解散队伍
    /// </summary>
    public sealed class DisbandRaidGroupPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClientBackendSession), nameof(ClientBackendSession.DisbandRaidGroup));

        [PatchPrefix]
        public static bool Prefix(ClientBackendSession __instance, Callback callback)
        {
            TasksExtensions.HandleExceptions(__instance.SendVoid(new SendRequest
            {
                Url = __instance.BackendUrls.Main + "/mcs/client/match/group/delete",
                Retries = SendRequest.NoRetries
            }, callback));
            return false;
        }
    }
}