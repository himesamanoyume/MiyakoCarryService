
using System.Reflection;
using HarmonyLib;
using MiyakoCarryService.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 玩家启动游戏时，始终进行一次小队成员清理
    /// </summary>
    [Injectable]
    public sealed class GameStartPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameCallbacks), nameof(GameCallbacks.GameStart));

        public GameStartPatch(RaidController raidController, ProfileController profileController)
        {
            _raidController = raidController;
            _profileController = profileController;
        }

        private static RaidController _raidController;
        private static ProfileController _profileController;

        [PatchPrefix]
        public static void Prefix(string url, EmptyRequestData _, MongoId sessionID)
        {
            _raidController.ClearGroupMember(sessionID);
            _profileController.RemoveMcsBotPlayerAid(sessionID);
        }
    }
}