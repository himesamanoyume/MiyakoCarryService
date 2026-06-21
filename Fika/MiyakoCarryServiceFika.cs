using System.Collections.Generic;
using MiyakoCarryService.Client.Enums;
using System;
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
using UnityEngine;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client;
using MiyakoCarryService.Fika.Patches;
using MiyakoCarryService.Client.Patches.Events;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika
{
    public class MiyakoCarryServiceFika
    {
        private Dictionary<ECommandPacketType, Action<CommandPacket>> _handleActionsMap;
        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        private SubtitlesMgr SubtitlesMgr => MgrAccessor.Get<SubtitlesMgr>();
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

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
            EventMgr.Subscribe<SubtitlesMgrHandleFikaEvent>(SendTalkMsgPacket, this);
            EventMgr.Subscribe<CommandMgrHandleFikaEvent>(SendCommandPacket, this);
            EventMgr.Subscribe<ConfigEntrySettingChangedEvent>(SendMcsBotPlayerConfigPacket, this);

            _handleActionsMap = new()
            {
                {ECommandPacketType.Teleport, HandleTeleport},
                {ECommandPacketType.GoToPoint, HandleGoToPoint},
                {ECommandPacketType.HoldPosition, HandleHoldPosition},
                {ECommandPacketType.Regroup, HandleRegroup},
                {ECommandPacketType.ReportAboutEnemy, HandleReportAboutEnemy},
                {ECommandPacketType.OnYourOwn, HandleOnYourOwn},
                {ECommandPacketType.Escort, HandleEscort},
                {ECommandPacketType.GoToExfil, HandleGoToExfil},
            };
        }

        public void CleanMcsFika()
        {
            foreach (var patch in _patches)
            {
                patch.Disable();
            }
            if (_handleActionsMap != null)
            {
                _handleActionsMap.Clear();
            }
            _handleActionsMap = null;
            FikaEventDispatcher.UnsubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
            EventMgr.Unsubscribe<SubtitlesMgrHandleFikaEvent>(SendTalkMsgPacket);
            EventMgr.Unsubscribe<CommandMgrHandleFikaEvent>(SendCommandPacket);
            EventMgr.Unsubscribe<ConfigEntrySettingChangedEvent>(SendMcsBotPlayerConfigPacket);
        }

        public void OnFikaNetworkCreated(FikaNetworkManagerCreatedEvent fikaEvent)
        {
            fikaEvent.Manager.RegisterPacket<CommandPacket>(OnCommandPacketReceived);
            fikaEvent.Manager.RegisterPacket<TalkMsgPacket>(OnTalkPacketReceived);
            fikaEvent.Manager.RegisterPacket<McsBotPlayerConfigPacket>(OnMcsBotPlayerConfigPacketReceived);

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

            if (_handleActionsMap.TryGetValue(packet.CommandType, out var action))
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

        private void HandleTeleport(CommandPacket packet)
        {
            if (packet.CommandType != ECommandPacketType.Teleport)
            {
                return;
            }

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

                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                botOwner.Mover.AllowTeleport();
                mcsBotPlayer.Teleport(mcsLeadPlayer.Position, true);
                var playerPosition = mcsBotPlayer.Position;
                botOwner.Mover.LastGoodCastPoint = botOwner.Mover.PrevSuccessLinkedFrom_1 = botOwner.Mover.PrevLinkPos = botOwner.Mover.PositionOnWayInner = playerPosition;
                botOwner.Mover.LastGoodCastPointTime = Time.time;
                botOwner.Mover.PrevPosLinkedTime_1 = 0f;
                botOwner.Mover.SetPlayerToNavMesh(playerPosition);
                botOwner.Mover.RecalcWay();
                botOwner.Mover.Pause = true;
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });
            }
        }

        private void HandleGoToPoint(CommandPacket packet)
        {
            if (packet.CommandType != ECommandPacketType.GoToPoint)
            {
                return;
            }

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

                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var pos = Tools.GetPosNearTarget(packet.Position.Value, botOwner);
                if (pos.HasValue)
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Going,
                    });
                    var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                    if (mcsBotPlayerData != null)
                    {
                        mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldGoToPoint);
                        mcsBotPlayerData.IsLooting = false;
                        mcsBotPlayerData.EscortPos = null;
                    }
                    botOwner.Mover.LastTimePosChanged = Time.time;
                    botOwner.StopMove();
                    botOwner.GoToSomePointData.SetPoint(pos.Value);
                }
            }
        }

        private void HandleEscort(CommandPacket packet)
        {
            if (packet.CommandType != ECommandPacketType.Escort)
            {
                return;
            }

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

                var botOwner = mcsBotPlayer.AIData.BotOwner;
                if (packet.Position.HasValue)
                {
                    if (botOwner.Memory.HaveEnemy)
                    {
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Negative,
                        });
                    }
                    botOwner.Mover.LastTimePosChanged = Time.time;
                    botOwner.StopMove();
                    var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                    if (mcsBotPlayerData != null)
                    {
                        mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldEscort);
                        mcsBotPlayerData.EscortPos = packet.Position.Value;
                        mcsBotPlayerData.IsLooting = false;
                    }
                }
            }
        }

        private void HandleGoToExfil(CommandPacket packet)
        {
            if (packet.CommandType != ECommandPacketType.GoToExfil)
            {
                return;
            }

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

                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.EscortPos = null;
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.SetDecision(null, EDecision.ShouldExfil);
                }
            }
        }

        private void HandleHoldPosition(CommandPacket packet)
        {
            if (packet.CommandType != ECommandPacketType.HoldPosition)
            {
                return;
            }

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

                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldHoldPosition);
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.HoldPosition,
                    });
                }
            }
        }

        private void HandleRegroup(CommandPacket packet)
        {
            if (packet.CommandType != ECommandPacketType.Regroup)
            {
                return;
            }

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

                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.SetDecision(null, EDecision.ShouldRegroup);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.EscortPos = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Regroup,
                });
            }
        }

        private void HandleReportAboutEnemy(CommandPacket packet)
        {
            if (packet.CommandType != ECommandPacketType.ReportAboutEnemy)
            {
                return;
            }

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

                var botOwner = mcsBotPlayer.AIData.BotOwner;
                if (botOwner.Memory.HaveEnemy)
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.OnFirstContact,
                        Position = botOwner.Memory.GoalEnemy.EnemyLastPosition
                    });
                }
                else
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Clear,
                    });
                }
            }
        }

        private void HandleOnYourOwn(CommandPacket packet)
        {
            if (packet.CommandType != ECommandPacketType.OnYourOwn)
            {
                return;
            }

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

                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.SetDecision();
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.EscortPos = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });
            }
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
                    CommandType = @event.CommandPacketType,
                    Position = @event.Position,
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
