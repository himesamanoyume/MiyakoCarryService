using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Patches.Group;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 当进入护航库存模式时，拦截并改为获取护航的存档
    /// </summary>
    public sealed class GetProfilesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Class308), nameof(Class308.GetProfiles));

        [PatchPrefix]
        public static bool Prefix(Class308 __instance, ref Task __result)
        {
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return true;
            }

            __result = GetMcsBotPlayerProfile(__instance);
            return false;
        }

        private static async Task GetMcsBotPlayerProfile(Class308 session)
        {
            var response = await McsRequestHandler.GetMcsBotPlayerProfiles();

            session.ProfilesUpdateTime = Time.time;

            session.AllProfiles = response
                .Select(descriptor => new Profile(descriptor))
                .ToArray();

            foreach (var profile in session.AllProfiles)
            {
                ItemFactoryClass.LogErrors(
                    profile.Inventory.DeserializationErrors,
                    "profile " + profile.Id
                );
                profile.EftStats.TotalInGameTime = session.TotalInGameTime;
            }

            session.Profile = session.AllProfiles.FirstOrDefault(p => p.Info.Side.CheckSide(EPlayerSideMask.Pmc));
            var scavProfile = session.AllProfiles.FirstOrDefault(p => p.Info.Side.CheckSide(EPlayerSideMask.Savage));
            var pmcProfile = session.Profile;
            session.OverallAccountStats = pmcProfile?.EftStats.OverallCounters.SumCounters(scavProfile?.EftStats.OverallCounters);
        }
    }
}