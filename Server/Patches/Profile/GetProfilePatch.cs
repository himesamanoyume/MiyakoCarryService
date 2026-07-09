
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;

namespace MiyakoCarryService.Server.Patches.Profile
{
    /// <summary>
    /// 检测到玩家处于护航库存模式时，改为获取护航的存档
    /// </summary>
    public sealed class GetProfilePatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SaveServer), nameof(SaveServer.GetProfile));

        public GetProfilePatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static Controllers.ProfileController ProfileController { get => field ??= ServiceProvider.GetService<Controllers.ProfileController>(); }

        [PatchPrefix]  
        public static bool Prefix(MongoId sessionId, ref SptProfile __result)  
        {  
            if (ProfileController.IsMcsBotPlayerInventoryMode(sessionId))  
            {  
                __result = ProfileController.GetMcsBotPlayerFullProfileForInventoryMode(sessionId);  
                return false;
            }  
            return true;  
        }
    }
}