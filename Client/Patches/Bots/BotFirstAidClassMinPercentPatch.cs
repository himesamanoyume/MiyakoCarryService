
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 让Bot治疗部位没有最低百分比要求
    /// </summary>
    public class BotFirstAidClassMinPercentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.PropertyGetter(typeof(BotFirstAidClass), nameof(BotFirstAidClass.min_percent));

        [PatchPrefix]
        public static bool Prefix(ref float __result)
        {
            __result = -1f;
            return false;
        }
    }
}