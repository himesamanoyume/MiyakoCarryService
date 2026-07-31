
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Servers;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 检测到玩家处于护航库存模式时，改为保存护航的存档
    /// </summary>
    [Injectable]
    public sealed class SaveProfileAsyncPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SaveServer), nameof(SaveServer.SaveProfileAsync));

        public SaveProfileAsyncPatch(Controllers.ProfileController profileController)
        {
            _profileController = profileController;
        }

        private static Controllers.ProfileController _profileController;

        [PatchPrefix]  
        public static bool Prefix(MongoId sessionID, ref Task<long> __result)  
        {  
            if (_profileController.IsMcsBotPlayerInventoryMode(sessionID))  
            {  
                __result = _profileController.SaveAllMcsBotPlayerProfile(sessionID);  
                return false;
            }  
            return true;  
        }
    }
}