
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.ChatBot;
using MiyakoCarryService.Server.Controllers;
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
            var mcsChatBot = ServiceLocator.ServiceProvider.GetService<MCSChatBot>();
            __result.Friends.Add(mcsChatBot.GetChatBot());

            var mcsProfileController = ServiceLocator.ServiceProvider.GetService<MCSProfileController>();
            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var profile = profileHelper.GetFullProfile(sessionId);

            var csPlayerProfiles = mcsProfileController.GetCSFullProfileByBossId(sessionId);

            if (csPlayerProfiles is not null)
            {
                foreach (var csPlayerProfile in csPlayerProfiles)
                {
                    if (csPlayerProfile is null)
                    {
                        continue;
                    }

                    var csPmcData = csPlayerProfile.CharacterData.PmcData;
                    var csSearchFriendResponse = new SearchFriendResponse
                    {
                        Id = csPmcData.Id.Value,
                        Aid = csPmcData.Aid,
                        Info = new UserDialogDetails
                        {
                            Nickname = csPmcData.Info.Nickname,
                            Side = csPmcData.Info.Side,
                            Level = csPmcData.Info.Level,
                            MemberCategory = csPmcData.Info.MemberCategory,
                            SelectedMemberCategory = csPmcData.Info.SelectedMemberCategory,
                        },
                    };

                    if (csSearchFriendResponse is not null)
                    {
                        __result.Friends.Add(
                            new UserDialogInfo
                            {
                                Id = csSearchFriendResponse.Id,
                                Aid = csSearchFriendResponse.Aid,
                                Info = csSearchFriendResponse.Info,
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