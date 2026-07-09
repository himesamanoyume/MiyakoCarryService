using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.Packets.Backend;
using HarmonyLib;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Patches.Events;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    /// <summary>
    /// 用于副机同步护航信息至主机
    /// </summary>
    public class OnPeerConnectedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(FikaClient), nameof(FikaClient.OnPeerConnected));

        [PatchPostfix]
        public static async void Postfix(FikaClient __instance, NetPeer peer)
        {
            await Task.Delay(1000);
            foreach (var groupPlayerViewModelClass in MatchmakerAcceptScreenShowPatch.GroupPlayers)
            {
                if (groupPlayerViewModelClass.Id == GameLoop.Instance.Session.Profile.Id)
                {
                    continue;
                }

                var profiles = new Dictionary<Profile, bool>();
                var completeProfileDescriptorClass = new ProfileDescriptor
                {
                    AccountId = groupPlayerViewModelClass.AccountId,
                    Id = groupPlayerViewModelClass.Id,
                    Info = new ProfileInfoDescriptor()
                    {
                        Level = groupPlayerViewModelClass.Info.Level,
                        Experience = ProfileInfo.GetExperience(groupPlayerViewModelClass.Info.Level),
                        PrestigeLevel = groupPlayerViewModelClass.Info.PrestigeLevel,
                        MemberCategory = groupPlayerViewModelClass.Info.MemberCategory,
                        SelectedMemberCategory = groupPlayerViewModelClass.Info.SelectedMemberCategory,
                        Nickname = groupPlayerViewModelClass.Info.Nickname,
                        MainProfileNickname = groupPlayerViewModelClass.Info.Nickname,
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
                        Equipment = ItemBinarySerializer.SerializeItem(groupPlayerViewModelClass.PlayerVisualRepresentation.Equipment, FullySearchedSearchController.Instance)
                    },
                    TaskConditionCounters = [],
                    Encyclopedia = []
                };

                var profile = new Profile(completeProfileDescriptorClass);
                profiles.Add(profile, false);
                var profilePacket = new LoadingProfilePacket()
                {
                    Profiles = profiles
                };
                __instance.SendData(ref profilePacket, DeliveryMethod.ReliableOrdered);
                await Task.Delay(1000);
            }
        }
    }
}