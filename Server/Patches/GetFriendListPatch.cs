
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.ChatBot;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.Patches
{
    public sealed class GetFriendListPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GetFriendList));

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, ref GetFriendListDataResponse __result)
        {
            var miyakoChatBot = ServiceLocator.ServiceProvider.GetService<MiyakoChatBot>();
            __result.Friends.Add(miyakoChatBot.GetChatBot());

            var profileController = ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>();
            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var profile = profileHelper.GetFullProfile(sessionId);

            var mcsBotPlayerProfiles = profileController.GetMcsPlayerProfileByBossId(sessionId);

            if (mcsBotPlayerProfiles is not null)
            {
                foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
                {
                    if (mcsBotPlayerProfile is null)
                    {
                        continue;
                    }

                    var mcsPmcData = mcsBotPlayerProfile.CharacterData.PmcData;
                    var searchFriendResponse = new SearchFriendResponse
                    {
                        Id = mcsPmcData.Id.Value,
                        Aid = mcsPmcData.Aid,
                        Info = new UserDialogDetails
                        {
                            Nickname = mcsPmcData.Info.Nickname,
                            Side = mcsPmcData.Info.Side,
                            Level = mcsPmcData.Info.Level,
                            MemberCategory = mcsPmcData.Info.MemberCategory,
                            SelectedMemberCategory = mcsPmcData.Info.SelectedMemberCategory,
                        },
                    };

                    if (searchFriendResponse is not null)
                    {
                        __result.Friends.Add(
                            new UserDialogInfo
                            {
                                Id = searchFriendResponse.Id,
                                Aid = searchFriendResponse.Aid,
                                Info = searchFriendResponse.Info,
                            }
                        );
                    }
                }
            }

            __result.Friends = __result.Friends
                .GroupBy(u => u.Aid)
                .Select(x => x.First())
                .ToList();
        }
    }
}