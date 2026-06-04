using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using HarmonyLib;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Patches.Events;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    /// <summary>
    /// 用于主机同步护航信息至副机
    /// </summary>
    public class FikaServerInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(FikaServer), nameof(FikaServer.Init));

        [PatchPostfix]
        public static async void Postfix(Task __result, Dictionary<Profile, bool> ____visualProfiles)
        {
            await __result;
            if (FikaBackendUtils.IsHeadless)
            {
                return;
            }
            foreach (var groupPlayerViewModelClass in MatchmakerAcceptScreenShowPatch.GroupPlayers)
            {
                if (groupPlayerViewModelClass.Id == GameLoop.Instance.Session.Profile.Id)
                {
                    continue;
                }

                var completeProfileDescriptorClass = new CompleteProfileDescriptorClass
                {
                    AccountId = groupPlayerViewModelClass.AccountId,
                    Id = groupPlayerViewModelClass.Id,
                    Info = new ProfileInfoClass()
                    {
                        Level = groupPlayerViewModelClass.Info.Level,
                        PrestigeLevel = groupPlayerViewModelClass.Info.PrestigeLevel,
                        MemberCategory = groupPlayerViewModelClass.Info.MemberCategory,
                        SelectedMemberCategory = groupPlayerViewModelClass.Info.SelectedMemberCategory,
                        Nickname = groupPlayerViewModelClass.Info.Nickname,
                        Side = groupPlayerViewModelClass.Info.Side,
                        GameVersion = groupPlayerViewModelClass.Info.GameVersion,
                        HasCoopExtension = groupPlayerViewModelClass.Info.HasCoopExtension,
                        SavageLockTime = groupPlayerViewModelClass.Info.SavageLockTime,
                    },
                    Customization = groupPlayerViewModelClass.PlayerVisualRepresentation.Customization,
                    Health = new(),
                    InsuredItems = [],
                    Inventory = new()
                    {
                        Equipment = EFTItemSerializerClass.SerializeItem(groupPlayerViewModelClass.PlayerVisualRepresentation.Equipment, GClass2240.Instance)
                    },
                    TaskConditionCounters = [],
                    Encyclopedia = []
                };

                var profile = new Profile(completeProfileDescriptorClass);
                ____visualProfiles.Add(profile, false);
            }
        }
    }
}