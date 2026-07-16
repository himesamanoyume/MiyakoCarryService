
using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MiyakoCarryService.Server.ChatBot;
using MiyakoCarryService.Server.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Services.Locales;

namespace MiyakoCarryService.Server.Patches.Friend
{
    /// <summary>
    /// 修复获取好友列表时的一些bug，并实现护航作为好友显示
    /// </summary>
    public sealed class GetFriendListPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GetFriendList));

        public GetFriendListPatch(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private static IServiceProvider ServiceProvider;

        private static MiyakoChatBot MiyakoChatBot { get => field ??= ServiceProvider.GetService<MiyakoChatBot>(); }
        private static Controllers.ProfileController ProfileController { get => field ??= ServiceProvider.GetService<Controllers.ProfileController>(); }
        private static Controllers.ConfigController ConfigController { get => field ??= ServiceProvider.GetService<Controllers.ConfigController>(); }
        private static Controllers.InfoController InfoController { get => field ??= ServiceProvider.GetService<Controllers.InfoController>(); }
        private static ServerLocalisationService ServerLocalisationService { get => field ??= ServiceProvider.GetService<ServerLocalisationService>(); }

        [PatchPostfix]
        public static void Postfix(MongoId sessionId, ref GetFriendListDataResponse __result)
        {
            __result.Friends.Add(MiyakoChatBot.GetChatBot());
            var mcsBotPlayerProfiles = ProfileController.GetAllMcsBotPlayerProfileByBossId(sessionId);

            if (mcsBotPlayerProfiles is not null)
            {
                foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
                {
                    if (mcsBotPlayerProfile is null)
                    {
                        continue;
                    }

                    var mcsPmcData = mcsBotPlayerProfile.CharacterData.PmcData;

                    var displayName = InfoController.IsOrderExpiredByBotPlayerProfileId(mcsPmcData.Id.Value) ? $"({ServerLocalisationService.GetText(Locales.MCSBOTPLAYEREXPIRED)}) {mcsPmcData.Info.Nickname}" : mcsPmcData.Info.Nickname;

                    var searchFriendResponse = new SearchFriendResponse
                    {
                        Id = mcsPmcData.Id.Value,
                        Aid = mcsPmcData.Aid,
                        Info = new UserDialogDetails
                        {
                            Nickname = displayName + $" [{ConfigController.GetSpawnTypeDisplayName(mcsPmcData.Info.Settings.Role)}]",
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