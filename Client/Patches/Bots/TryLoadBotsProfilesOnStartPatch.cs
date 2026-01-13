
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 从服务端获取所有要生成的护航Bot数据
    /// </summary>
    internal sealed class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsPresets), nameof(BotsPresets.TryLoadBotsProfilesOnStart));

        [PatchPostfix]
        public static async void Postfix(Task __result, List<Profile> ___List_0)
        {
            await __result;
            var csProfiles = await McsRequestHandler.GetCarryServicePlayer();
            // ___List_0.InsertRange(0, csProfiles); // 可能有性能影响
            ___List_0.AddRange(csProfiles);
        }
    }
}