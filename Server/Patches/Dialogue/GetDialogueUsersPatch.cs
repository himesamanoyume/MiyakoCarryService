
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace MiyakoCarryService.Server.Patches.Dialogue
{
    /// <summary>
    /// 确保当对护航发送消息时能够拥有保持正常
    /// </summary>
    public sealed class GetDialogueUsersPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GetDialogueUsers));

        [PatchPrefix]
        public static bool Prefix(SPTarkov.Server.Core.Models.Eft.Profile.Dialogue? dialog, MessageType? messageType, MongoId sessionId, ref List<UserDialogInfo> __result)
        {
            var orderInfoController = ServiceLocator.ServiceProvider.GetService<Controllers.OrderInfoController>();

            if (orderInfoController.CheckMcsBotPlayerExist(sessionId))
            {
                var profileController = ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>();
                var profile = profileController.GetMcsBotPlayerProfileByBotId(sessionId);

                if (
                    messageType == MessageType.UserMessage
                    && dialog?.Users is not null
                    && dialog.Users.All(userDialog => userDialog.Id != profile.CharacterData?.PmcData?.SessionId)
                )
                {
                    dialog.Users.Add(
                        new UserDialogInfo
                        {
                            Id = profile.CharacterData.PmcData.SessionId.Value,
                            Aid = profile.CharacterData?.PmcData?.Aid,
                            Info = new UserDialogDetails
                            {
                                Level = profile.CharacterData?.PmcData?.Info?.Level,
                                Nickname = profile.CharacterData?.PmcData?.Info?.Nickname,
                                Side = profile.CharacterData?.PmcData?.Info?.Side,
                                MemberCategory = profile.CharacterData?.PmcData?.Info?.MemberCategory,
                                SelectedMemberCategory = profile.CharacterData?.PmcData?.Info?.SelectedMemberCategory,
                            },
                        }
                    );
                }

                __result = dialog?.Users!;
                return false;
            }
            return true;
        }
    }
}