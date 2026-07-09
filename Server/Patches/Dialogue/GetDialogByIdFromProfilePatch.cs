
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace MiyakoCarryService.Server.Patches.Dialogue
{
    /// <summary>
    /// 确保当对护航发送消息时能够获取正确数据
    /// </summary>
    public sealed class GetDialogByIdFromProfilePatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), "GetDialogByIdFromProfile");

        private static Controllers.InfoController InfoController { get => field ??= ServiceLocator.ServiceProvider.GetService<Controllers.InfoController>(); }

        [PatchPrefix]
        public static bool Prefix(SptProfile profile, GetMailDialogViewRequestData request, ref SPTarkov.Server.Core.Models.Eft.Profile.Dialogue __result)
        {
            if (InfoController.CheckMcsBotPlayerExist(request.DialogId))
            {
                if (profile.DialogueRecords is null || profile.DialogueRecords.ContainsKey(request.DialogId))
                {
                    __result = profile.DialogueRecords?[request.DialogId] ?? throw new NullReferenceException();
                    return false;
                }

                profile.DialogueRecords[request.DialogId] = new SPTarkov.Server.Core.Models.Eft.Profile.Dialogue
                {
                    Id = request.DialogId,
                    AttachmentsNew = 0,
                    Pinned = false,
                    Messages = [],
                    New = 0,
                    Type = request.Type,
                };

                if (request.Type != MessageType.UserMessage)
                {
                    __result = profile.DialogueRecords[request.DialogId];
                    return false;
                }

                var dialogue = profile.DialogueRecords[request.DialogId];
                dialogue.Users = [];

                var profileController = ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>();
                var mcsBotPlayerProfile = profileController.GetMcsBotPlayerProfileByBotId(request.DialogId);

                if (mcsBotPlayerProfile is null)
                {
                    __result = profile.DialogueRecords[request.DialogId];
                    return false;
                }

                dialogue.Users ??= [];
                dialogue.Users.Add(new UserDialogInfo
                {
                    Id = mcsBotPlayerProfile.CharacterData.PmcData.SessionId.Value,
                    Aid = mcsBotPlayerProfile.CharacterData?.PmcData?.Aid,
                    Info = new UserDialogDetails
                    {
                        Level = mcsBotPlayerProfile.CharacterData?.PmcData?.Info?.Level,
                        Nickname = mcsBotPlayerProfile.CharacterData?.PmcData?.Info?.Nickname,
                        Side = mcsBotPlayerProfile.CharacterData?.PmcData?.Info?.Side,
                        MemberCategory = mcsBotPlayerProfile.CharacterData?.PmcData?.Info?.MemberCategory,
                        SelectedMemberCategory = mcsBotPlayerProfile.CharacterData?.PmcData?.Info?.SelectedMemberCategory,
                    }
                });
                __result = dialogue;
                return false;
            }
            return true;
        }
    }
}