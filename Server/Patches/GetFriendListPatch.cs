
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
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
            var mcsProfileController = ServiceLocator.ServiceProvider.GetService<MCSProfileController>();
            var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
            var profile = profileHelper.GetFullProfile(sessionId);

            foreach (var friendId in profile.FriendProfileIds)
            {
                var csProfile = mcsProfileController.GetCSFullProfile(sessionId, friendId).CharacterData.PmcData;
                var csSearchFriendResponse = new SearchFriendResponse
                {
                    Id = csProfile.Id.Value,
                    Aid = csProfile.Aid,
                    Info = new UserDialogDetails
                    {
                        Nickname = csProfile.Info.Nickname,
                        Side = csProfile.Info.Side,
                        Level = csProfile.Info.Level,
                        MemberCategory = csProfile.Info.MemberCategory,
                        SelectedMemberCategory = csProfile.Info.SelectedMemberCategory,
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
    }
}