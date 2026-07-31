
using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 检测到玩家处于护航库存模式时，改为获取护航的存档
    /// </summary>
    [Injectable]
    public sealed class GetProfilePatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SaveServer), nameof(SaveServer.GetProfile));

        public GetProfilePatch(Controllers.ProfileController profileController)
        {
            _profileController = profileController;
        }

        private static Controllers.ProfileController _profileController;

        [PatchPrefix]
        public static bool Prefix(MongoId sessionId, ref SptProfile __result)
        {
            if (_profileController.IsMcsBotPlayerInventoryMode(sessionId))
            {
                __result = _profileController.GetMcsBotPlayerFullProfileForInventoryMode(sessionId);;
                return false;
            }
            return true;
        }
    }
}