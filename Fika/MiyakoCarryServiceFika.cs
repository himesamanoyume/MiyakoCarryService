using System.Collections.Generic;
using MiyakoCarryService.Fika.Packets;
using MiyakoCarryService.Client.Mgrs;
using Fika.Core.Modding.Events;
using Fika.Core.Main.Utils;
using Comfort.Common;
using Fika.Core.Networking;
using Fika.Core.Main.Players;
using Fika.Core.Modding;
using EFT;
using Fika.Core.Networking.LiteNetLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client;
using MiyakoCarryService.Fika.Patches;
using MiyakoCarryService.Client.Patches.Events;
using HarmonyLib;
using SPT.Reflection.Patching;
using MiyakoCarryService.Client.Api;
using BepInEx;

namespace MiyakoCarryService.Fika
{
    [BepInPlugin(McsFikaGUID, McsFikaName, MiyakoCarryServicePlugin.BepInExClientVersion)]
    [BepInProcess(MiyakoCarryServicePlugin.EFTapp)]
    [BepInDependency(MiyakoCarryServicePlugin.BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MiyakoCarryServicePlugin.McsGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MiyakoCarryServicePlugin.FikaGUID, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class MiyakoCarryServiceFika : BaseUnityPlugin
    {
        private SubtitlesMgr SubtitlesMgr => McsMgrApi.GetMgr<SubtitlesMgr>();
        private QuestDataMgr QuestDataMgr => McsMgrApi.GetMgr<QuestDataMgr>();
        private List<ModulePatch> _patches = new();
        public const string McsFikaGUID = "top.himesamanoyume.miyakocarryservice.fika";
#if DEBUG
        public const string McsFikaName = "姫様の夢 MiyakoCarryServiceFika DebugBuild";
#else
        public const string McsFikaName = "姫様の夢 MiyakoCarryServiceFika";
#endif

        void Start()
        {
            _patches.Add(new ExtractPatch());
            _patches.Add(new OnLoadingProfilePacketReceivedPatch());
            _patches.Add(new OnPeerConnectedPatch());
            _patches.Add(new FikaOnBeenKilledByAggressorPatch1());
            _patches.Add(new FikaOnBeenKilledByAggressorPatch2());
            _patches.Add(new SetupCorpseSyncPacketPatch());

            foreach (var patch in _patches)
            {
                patch.Enable();
            }

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
            McsEventApi.Subscribe<SubtitlesMgrHandleFikaEvent>(SendTalkMsgPacket, this);
            McsEventApi.Subscribe<QuestProxyCommandCallbackHandleFikaEvent>(SendQuestProxyCommandCallbackPacket, this);
            McsEventApi.Subscribe<CommandMgrHandleFikaEvent>(SendCommandPacket, this);
            McsEventApi.Subscribe<ConfigEntrySettingChangedEvent>(SendMcsBotPlayerConfigPacket, this);
        }

        public void OnDestroy()
        {
            foreach (var patch in _patches)
            {
                patch.Disable();
            }
            FikaEventDispatcher.UnsubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
            McsEventApi.Unsubscribe<SubtitlesMgrHandleFikaEvent>(SendTalkMsgPacket);
            McsEventApi.Unsubscribe<CommandMgrHandleFikaEvent>(SendCommandPacket);
            McsEventApi.Unsubscribe<ConfigEntrySettingChangedEvent>(SendMcsBotPlayerConfigPacket);
            McsEventApi.Unsubscribe<QuestProxyCommandCallbackHandleFikaEvent>(SendQuestProxyCommandCallbackPacket);
        }

        public void OnFikaNetworkCreated(FikaNetworkManagerCreatedEvent fikaEvent)
        {
            fikaEvent.Manager.RegisterPacket<CommandPacket>(OnCommandPacketReceived);
            fikaEvent.Manager.RegisterPacket<TalkMsgPacket>(OnTalkPacketReceived);
            fikaEvent.Manager.RegisterPacket<McsBotPlayerConfigPacket>(OnMcsBotPlayerConfigPacketReceived);
            fikaEvent.Manager.RegisterPacket<QuestProxyCommandCallbackPacket>(OnQuestProxyCommandCallbackPacketReceived);

            // 用于主机同步护航信息至副机
            if (fikaEvent.Manager is FikaServer fikaServer && !FikaBackendUtils.IsHeadless)
            {
                var visualProfiles = (Dictionary<Profile, bool>)AccessTools.Field(typeof(FikaServer), "_visualProfiles").GetValue(fikaServer);

                foreach (var groupPlayerViewModelClass in MatchmakerAcceptScreenShowPatch.GroupPlayers)
                {
                    if (groupPlayerViewModelClass.Id == GameLoop.Instance.Session.Profile.Id)
                    {
                        continue;
                    }

                    try
                    {
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
                        visualProfiles.Add(profile, false);
                    }
                    catch
                    {

                    }

                }
            }
        }

        public void OnCommandPacketReceived(CommandPacket packet)
        {
            if (!FikaBackendUtils.IsServer)
            {
                return;
            }

            HandleCommandPacket(packet);
        }

        public void HandleCommandPacket(CommandPacket packet)
        {
            if (!FikaBackendUtils.IsServer)
            {
                return;
            }

            var fikaInstance = Singleton<IFikaNetworkManager>.Instance;

            fikaInstance.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null)
            {
                return;
            }

            if (fikaInstance.CoopHandler.Players.TryGetValue(packet.McsBotPlayerNetId, out FikaPlayer mcsBotPlayer))
            {
                if (!mcsBotPlayer.HealthController.IsAlive)
                {
                    return;
                }

                McsPacketApi.ExecuteCommand(packet.Payload, mcsLeadPlayer, mcsBotPlayer);
            }
        }

        public void OnTalkPacketReceived(TalkMsgPacket packet)
        {
            if (!FikaBackendUtils.IsClient)
            {
                return;
            }

            var fikaInstance = Singleton<IFikaNetworkManager>.Instance;
            fikaInstance.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null || !mcsLeadPlayer.IsYourPlayer)
            {
                return;
            }

            if (fikaInstance.CoopHandler.Players.TryGetValue(packet.McsBotPlayerNetId, out FikaPlayer mcsBotPlayer))
            {
                if (!mcsBotPlayer.HealthController.IsAlive)
                {
                    return;
                }

                SubtitlesMgr.ShowMsg(mcsLeadPlayer, mcsBotPlayer, McsPacketApi.DeserializeMsg(packet.Payload));
            }
        }

        public void OnQuestProxyCommandCallbackPacketReceived(QuestProxyCommandCallbackPacket packet)
        {
            if (!FikaBackendUtils.IsClient)
            {
                return;
            }

            var fikaInstance = Singleton<IFikaNetworkManager>.Instance;

            fikaInstance.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null || !mcsLeadPlayer.IsYourPlayer)
            {
                return;
            }

            if (fikaInstance.CoopHandler.Players.TryGetValue(packet.McsBotPlayerNetId, out FikaPlayer mcsBotPlayer))
            {
                if (!mcsBotPlayer.HealthController.IsAlive)
                {
                    return;
                }

                var targetId = McsPacketApi.DeserializeQuestProxy(packet.Payload);
                if (targetId == null)
                {
                    return;
                }

                var questData = QuestDataMgr.FindQuestData(targetId);
                if (questData != null)
                {
                    TasksExtensions.HandleExceptions(questData.ForceCompleteQuest(mcsBotPlayer));
                }
            }
        }

        public void OnMcsBotPlayerConfigPacketReceived(McsBotPlayerConfigPacket packet)
        {
            if (!FikaBackendUtils.IsServer)
            {
                return;
            }

            var fikaInstance = Singleton<IFikaNetworkManager>.Instance;
            fikaInstance.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null || mcsLeadPlayer.IsYourPlayer)
            {
                return;
            }

            McsPacketApi.ApplyBotPlayerConfig(mcsLeadPlayer.ProfileId, packet.Payload);
        }

        public void SendCommandPacket(CommandMgrHandleFikaEvent @event)
        {
            if (!FikaBackendUtils.IsClient)
            {
                return;
            }

            var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && @event.McsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                var packet = new CommandPacket
                {
                    Payload = McsPacketApi.SerializeCommand(@event),
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        public void SendTalkMsgPacket(SubtitlesMgrHandleFikaEvent @event)
        {
            if (!FikaBackendUtils.IsServer)
            {
                return;
            }

            var mcsLeadPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(@event.McsLeadPlayerId);
            var mcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(@event.McsBotPlayerId);
            if (mcsLeadPlayer == null || mcsBotPlayer == null)
            {
                return;
            }

            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                var packet = new TalkMsgPacket
                {
                    Payload = McsPacketApi.SerializeMsg(@event.Msg),
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };

                var netPeer = GetPeerByNetId(fikaMcsLeadPlayer.NetId);
                if (netPeer == null)
                {
                    Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                    return;
                }

                Singleton<IFikaNetworkManager>.Instance.SendDataToPeer(ref packet, DeliveryMethod.ReliableOrdered, netPeer);
            }
        }

        public void SendQuestProxyCommandCallbackPacket(QuestProxyCommandCallbackHandleFikaEvent @event)
        {
            if (!FikaBackendUtils.IsServer)
            {
                return;
            }

            var mcsLeadPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(@event.McsLeadPlayerId);
            var mcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(@event.McsBotPlayerId);
            if (mcsLeadPlayer == null || mcsBotPlayer == null)
            {
                return;
            }

            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                var packet = new QuestProxyCommandCallbackPacket
                {
                    Payload = McsPacketApi.SerializeQuestProxy(@event.TargetId),
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };

                var netPeer = GetPeerByNetId(fikaMcsLeadPlayer.NetId);
                if (netPeer == null)
                {
                    Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                    return;
                }

                Singleton<IFikaNetworkManager>.Instance.SendDataToPeer(ref packet, DeliveryMethod.ReliableOrdered, netPeer);
            }
        }

        public void SendMcsBotPlayerConfigPacket(ConfigEntrySettingChangedEvent @event)
        {
            if (!FikaBackendUtils.IsClient)
            {
                return;
            }

            var mcsLeadPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(@event.McsBotPlayerConfig.McsLeadPlayerId);
            if (mcsLeadPlayer == null)
            {
                return;
            }
            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer)
            {
                var packet = new McsBotPlayerConfigPacket
                {
                    Payload = McsPacketApi.SerializeBotPlayerConfig(@event.McsBotPlayerConfig),
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        private NetPeer GetPeerByNetId(int netId)
        {
            if (Singleton<IFikaNetworkManager>.Instance is not FikaServer server)
            {
                return null;
            }

            foreach (var peer in server.NetServer)
            {
                if (peer is NetPeer netPeer && netPeer.Player != null && netPeer.Player.NetId == netId)
                {
                    return netPeer;
                }
            }

            return null;
        }
    }
}
