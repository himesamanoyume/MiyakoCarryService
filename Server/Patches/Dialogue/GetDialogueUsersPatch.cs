
// using System;
// using System.Reflection;
// using HarmonyLib;
// using Microsoft.Extensions.DependencyInjection;
// using MiyakoCarryService.Server.Controllers;
// using SPTarkov.Reflection.Patching;
// using SPTarkov.Server.Core.Controllers;
// using SPTarkov.Server.Core.DI;
// using SPTarkov.Server.Core.Helpers;
// using SPTarkov.Server.Core.Models.Common;
// using SPTarkov.Server.Core.Models.Eft.Common;
// using SPTarkov.Server.Core.Models.Eft.Profile;
// using SPTarkov.Server.Core.Models.Eft.Quests;
// using SPTarkov.Server.Core.Models.Enums;

// namespace MiyakoCarryService.Server.Patches
// {
//     /// <summary>
//     /// 确保当对护航发送消息时能够生成正确的数据格式
//     /// </summary>
//     public sealed class GetDialogueUsersPatch : AbstractPatch
//     {
//         protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GetDialogueUsers));

//         [PatchPostfix]
//         public static void Postfix(Dialogue? dialog, MessageType? messageType, MongoId sessionId)
//         {
//             var profileController = ServiceLocator.ServiceProvider.GetService<Controllers.ProfileController>();
//             var profileHelper = ServiceLocator.ServiceProvider.GetService<ProfileHelper>();
//             var profile = profileHelper.GetFullProfile(sessionId);
//             var csProfile = profileController.GetMcsPlayerFullProfile(sessionId, friendId);

//             foreach (var friendId in profile.FriendProfileIds)
//             {
//             }
//         }
//     }
// }