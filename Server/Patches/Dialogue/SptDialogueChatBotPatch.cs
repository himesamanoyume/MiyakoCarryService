
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace MiyakoCarryService.Server.Patches.Dialogue
{
    /// <summary>
    /// 修复SPT自带的Bot的重复Aid问题
    /// </summary>
    public sealed class SptDialogueChatBotPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SptDialogueChatBot), nameof(SptDialogueChatBot.GetChatBot));

        private static CoreConfig _coreConfig;

        [PatchPrefix]
        public static bool Prefix(ref UserDialogInfo __result)
        {
            if (_coreConfig is null)
            {
                var configServer = ServiceLocator.ServiceProvider.GetService<ConfigServer>();
                _coreConfig = configServer.GetConfig<CoreConfig>();
            }

            __result = new UserDialogInfo
            {
                Id = _coreConfig.Features.ChatbotFeatures.Ids["spt"],
                Aid = 1234568,
                Info = new UserDialogDetails
                {
                    Level = 1,
                    MemberCategory = MemberCategory.Developer,
                    SelectedMemberCategory = MemberCategory.Developer,
                    Nickname = _coreConfig.SptFriendNickname,
                    Side = "Usec",
                },
            };

            return false;
        }
    }
}