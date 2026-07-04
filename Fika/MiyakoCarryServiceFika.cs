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
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client;
using MiyakoCarryService.Fika.Patches;
using MiyakoCarryService.Client.Patches.Events;
using HarmonyLib;
using SPT.Reflection.Patching;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Fika.Utils;
using MiyakoCarryService.Fika.Mgrs;

namespace MiyakoCarryService.Fika
{
    public class MiyakoCarryServiceFika
    {
        private McsMgr McsMgr => McsMgrApi.GetMgr<McsMgr>();
        private SubtitlesMgr SubtitlesMgr => McsMgrApi.GetMgr<SubtitlesMgr>();
        private QuestDataMgr QuestDataMgr => McsMgrApi.GetMgr<QuestDataMgr>();
        private List<ModulePatch> _patches = new();

        public void InitMcsFika()
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

            BaseMgr.Enable(typeof(CommandPacketMgr));

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
            McsEventApi.Subscribe<SubtitlesMgrHandleFikaEvent>(SendTalkMsgPacket, this);
            McsEventApi.Subscribe<QuestProxyCommandCallbackHandleFikaEvent>(SendQuestProxyCommandCallbackPacket, this);
            McsEventApi.Subscribe<CommandMgrHandleFikaEvent>(SendCommandPacket, this);
            McsEventApi.Subscribe<ConfigEntrySettingChangedEvent>(SendMcsBotPlayerConfigPacket, this);
        }

        public void CleanMcsFika()
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

                    var completeProfileDescriptorClass = new CompleteProfileDescriptorClass
                    {
                        AccountId = groupPlayerViewModelClass.AccountId,
                        Id = groupPlayerViewModelClass.Id,
                        Info = new ProfileInfoClass()
                        {
                            Level = groupPlayerViewModelClass.Info.Level,
                            Experience = InfoClass.GetExperience(groupPlayerViewModelClass.Info.Level),
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
                    visualProfiles.Add(profile, false);
                }
            }
        }

        public void OnCommandPacketReceived(CommandPacket packet)
        {
            if (!FikaBackendUtils.IsServer)
            {
                return;
            }

            var handleActionsMap = CommandPacketUtils.GetHandleActionsMap();

            if (handleActionsMap.TryGetValue(packet.CommandType, out var action))
            {
                action(packet);
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

                SubtitlesMgr.ShowMsg(mcsLeadPlayer, mcsBotPlayer, new McsMsg
                {
                    PhraseTrigger = packet.PhraseTrigger,
                    Position = packet.Position,
                    Key = packet.Key,
                    Key2 = packet.Key2
                });
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

                var questData = QuestDataMgr.FindQuestData(packet.TargetId);
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

            McsMgr.UpdateMcsBotPlayerConfig(mcsLeadPlayer.ProfileId, new McsBotPlayerConfig
            {
                McsLeadPlayerId = mcsLeadPlayer.ProfileId,
                EnableLooting = packet.McsBotPlayerConfig.EnableLooting,
                PriceThreshold = packet.McsBotPlayerConfig.PriceThreshold,
                KeywordItemText = packet.KeywordItemText,
                LootingKeywordItem = packet.McsBotPlayerConfig.LootingKeywordItem,
                BlockItemType = packet.McsBotPlayerConfig.BlockItemType,
            });
        }

        public virtual void SendCommandPacket(CommandMgrHandleFikaEvent @event)
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
                    CommandType = @event.CommandPacketType,
                    Position = @event.Position,
                    TargetId = @event.TargetId,
                    AimingBodyPartType = @event.AimingBodyPartType,
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        public virtual void SendTalkMsgPacket(SubtitlesMgrHandleFikaEvent @event)
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
                    PhraseTrigger = @event.Msg.PhraseTrigger,
                    Position = @event.Msg.Position,
                    Key = @event.Msg.Key,
                    Key2 = @event.Msg.Key2,
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };

                // 为了适配老版本Fika无法获取NetPeer，使用流量损耗更大的广播方式
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            }
        }

        public virtual void SendQuestProxyCommandCallbackPacket(QuestProxyCommandCallbackHandleFikaEvent @event)
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
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId,
                    TargetId = @event.TargetId
                };

                // 为了适配老版本Fika无法获取NetPeer，使用流量损耗更大的广播方式
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            }
        }

        public virtual void SendMcsBotPlayerConfigPacket(ConfigEntrySettingChangedEvent @event)
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
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    KeywordItemText = MiyakoCarryServicePlugin.KeywordItemText.Value,
                    McsBotPlayerConfig = new SMcsBotPlayerConfig
                    {
                        EnableLooting = MiyakoCarryServicePlugin.EnableLooting.Value,
                        PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
                        LootingKeywordItem = MiyakoCarryServicePlugin.LootingKeywordItem.Value,
                        BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value
                    }
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}
