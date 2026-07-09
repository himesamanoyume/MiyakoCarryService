
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Servers;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 检测到玩家处于护航库存模式时，改为保存护航的存档
    /// </summary>
    public sealed class SaveProfileAsyncPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SaveServer), nameof(SaveServer.SaveProfileAsync));

        private static Controllers.ProfileController ProfileController { get => field ??= ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>(); }

        [PatchPrefix]  
        public static bool Prefix(MongoId sessionID, ref Task<long> __result)  
        {  
            if (ProfileController.IsMcsBotPlayerInventoryMode(sessionID))  
            {  
                __result = ProfileController.SaveAllMcsBotPlayerProfile(sessionID);  
                return false;
            }  
            return true;  
        }
    }
}