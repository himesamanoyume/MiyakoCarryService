using System.Collections.Generic;
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SessionBackendClass), nameof(SessionBackendClass.GetProfiles));

        [PatchPrefix]
        public static bool Prefix(SessionBackendClass __instance, ref Task __result)
        {
            if (!GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode)
            {
                return true;
            }

            __result = GetMcsBotPlayerProfile(__instance);
            return false;
        }

        [PatchPostfix]
        public static async void Postfix(SessionBackendClass __instance, Task __result)
        {
            await __result;
            var profileStatuses = new List<ProfileStatusClass>();
            foreach (var profile in __instance.AllProfiles)
            {
                profileStatuses.Add(new()
                {
                    profileid = profile.Id,
                    status = EProfileStatus.Free,
                });
            }
            __instance.AllProfileStatus = profileStatuses.ToArray();
        }

        private static async Task GetMcsBotPlayerProfile(SessionBackendClass session)
        {
            var response = await McsRequestHandler.GetMcsBotPlayerProfiles();

            if (response == null || response.Length == 0)
            {
                GetContextInteractionsPatch.IsMcsBotPlayerInventoryMode = false;
                NotificationManagerClass.DisplayMessageNotification("未获取到护航存档，无法加载");
                return;
            }

            session.ProfilesUpdateTime = Time.time;
            session.AllProfiles = response.Select(descriptor => new Profile(descriptor)).ToArray();
            session.Profile = session.AllProfiles[0];
        }
    }
}