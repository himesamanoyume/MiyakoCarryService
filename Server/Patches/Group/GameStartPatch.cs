
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 玩家启动游戏时，始终进行一次小队成员清理
    /// </summary>
    public sealed class GameStartPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameCallbacks), nameof(GameCallbacks.GameStart));

        public GameStartPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static RaidController RaidController { get => field ??= ServiceProvider.GetService<RaidController>(); }
        private static ProfileController ProfileController { get => field ??= ServiceProvider.GetService<ProfileController>(); }

        [PatchPrefix]
        public static void Prefix(string url, EmptyRequestData _, MongoId sessionID)
        {
            RaidController.ClearGroupMember(sessionID);
            ProfileController.RemoveMcsBotPlayerAid(sessionID);
        }
    }
}