
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Routers;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 修复护航库存模式下交易等行为不会刷新库存数据的问题
    /// </summary>
    public sealed class ItemEventRouterHandleEventsPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemEventRouter), nameof(ItemEventRouter.HandleEvents));

        private static ProfileService ProfileService { get => field ??= ServiceLocator.ServiceProvider.GetService<ProfileService>(); }

        [PatchPostfix]
        public static void Postfix(MongoId sessionID, ref ValueTask<ItemEventRouterResponse> __result)
        {
            if (!ProfileService.IsMcsBotPlayerInventoryMode(sessionID)) 
            {
                return;
            }

            var mcsBotProfiles = ProfileService.GetMcsBotPlayerProfileForInventoryMode(sessionID);
            if (mcsBotProfiles is null || mcsBotProfiles.Count == 0) 
            {
                return;
            }

            var mcsBotProfileId = mcsBotProfiles[0].Id;
            __result = ReplaceProfileChangesKey(__result, sessionID, mcsBotProfileId.Value);
        }

        private static async ValueTask<ItemEventRouterResponse> ReplaceProfileChangesKey(ValueTask<ItemEventRouterResponse> originalTask, MongoId originalKey, MongoId newKey)
        {
            var response = await originalTask;

            if (response.ProfileChanges != null && response.ProfileChanges.TryGetValue(originalKey, out var profileChange))
            {
                response.ProfileChanges.Remove(originalKey);
                profileChange.Id = newKey;
                response.ProfileChanges[newKey] = profileChange;
            }

            return response;
        }
    }
}