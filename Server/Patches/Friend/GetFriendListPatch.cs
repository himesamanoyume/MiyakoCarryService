
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.ChatBot;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.Patches.Friend
{
    /// <summary>
    /// 修复获取好友列表时的一些bug，并实现护航作为好友显示
    /// </summary>
    public sealed class GetFriendListPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GetFriendList));

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, ref GetFriendListDataResponse __result)
        {
            var miyakoChatBot = ServiceLocator.ServiceProvider.GetService<MiyakoChatBot>();
            __result.Friends.Add(miyakoChatBot.GetChatBot());

            var profileController = ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>();
            var mcsBotPlayerProfiles = profileController.GetAllMcsBotPlayerProfileByBossId(sessionId);

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

                    __result.Friends = __result.Friends
                        .GroupBy(u => u.Aid)
                        .SelectMany(g =>
                        {
                            var list = g.ToList();
                            for (int i = 1; i < list.Count; i++)
                            {
                                list[i].Aid = g.Key + i;
                            }
                            return list;
                        })
                        .ToList();
                }
            }
        }
    }
}