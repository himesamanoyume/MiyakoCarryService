
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.HttpResponse;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 修复护航库存模式下交易等行为不会刷新库存数据的问题
    /// </summary>
    [Injectable]
    public sealed class ItemEventRouterHandleEventsPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemEventCallbacks), nameof(ItemEventCallbacks.HandleEvents));

        public ItemEventRouterHandleEventsPatch(ProfileService profileService, JsonUtil jsonUtil, HttpResponseUtil httpResponseUtil)
        {
            _profileService = profileService;
            _jsonUtil = jsonUtil;
            _httpResponseUtil = httpResponseUtil;
        }

        private static ProfileService _profileService;
        private static JsonUtil _jsonUtil;
        private static HttpResponseUtil _httpResponseUtil;


        [PatchPostfix]
        public static void Postfix(string url, ItemEventRouterRequest info, MongoId sessionID,
            CancellationToken cancellationToken, ref ValueTask<string> __result)
        {
            if (!_profileService.IsMcsBotPlayerInventoryMode(sessionID)) 
            {
                return;
            }

            var mcsBotProfiles = _profileService.GetMcsBotPlayerProfileForInventoryMode(sessionID);
            if (mcsBotProfiles is null || mcsBotProfiles.Count == 0) 
            {
                return;
            }

            var mcsBotProfileId = mcsBotProfiles[0].Id;
            __result = ReplaceProfileChangesKey(__result, sessionID, mcsBotProfileId.Value);
        }

        private static async ValueTask<string> ReplaceProfileChangesKey(ValueTask<string> originalTask, MongoId originalKey, MongoId newKey)
        {
            var response = await originalTask;
            var wrapper = _jsonUtil.Deserialize<GetBodyResponseData<ItemEventRouterResponse>>(response);
            var restored = wrapper?.Data ?? new ItemEventRouterResponse();

            if (restored.ProfileChanges != null && restored.ProfileChanges.TryGetValue(originalKey, out var profileChange))
            {
                restored.ProfileChanges.Remove(originalKey);
                profileChange.Id = newKey;
                restored.ProfileChanges[newKey] = profileChange;
            }

            return _httpResponseUtil.GetBody(restored);
        }
    }
}