
using System.Reflection;
using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 自己实现解散队伍
    /// </summary>
    internal sealed class DisbandRaidGroupPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ProfileEndpointFactoryAbstractClass), nameof(ProfileEndpointFactoryAbstractClass.DisbandRaidGroup));

        [PatchPrefix]
        public static bool Prefix(ProfileEndpointFactoryAbstractClass __instance, Callback callback)
        {
            TasksExtensions.HandleExceptions(__instance.method_6(new LegacyParamsStruct
            {
                Url = __instance.Gclass1392_0.Main + "/mcs/client/match/group/delete",
                Retries = LegacyParamsStruct.NoRetries
            }, callback));
            return false;
        }
    }
}