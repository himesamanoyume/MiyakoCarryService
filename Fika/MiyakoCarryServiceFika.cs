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
using UnityEngine.AI;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client;

namespace MiyakoCarryService.Fika
{
    public class MiyakoCarryServiceFika
    {
        private Dictionary<ECommandPacketType, Action<CommandPacket>> _handleActionsMap;
        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        private SubTitleMgr SubTitleMgr => MgrAccessor.Get<SubTitleMgr>();

        public void InitMcsFika()
        {
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
            EventMgr.Subscribe<SubTitleMgrHandleFikaEvent>(SendTalkPacket, this);
            EventMgr.Subscribe<CommandMgrHandleFikaEvent>(SendCommandPacket, this);
            EventMgr.Subscribe<ConfigEntrySettingChangedEvent>(SendMcsBotPlayerConfigPacket, this);
            _handleActionsMap = new()
            {
                {ECommandPacketType.Teleport, HandleTeleport},
                {ECommandPacketType.GoToPoint, HandleGoToPoint},
                {ECommandPacketType.HoldPosition, HandleHoldPosition},
                {ECommandPacketType.Regroup, HandleRegroup},
                {ECommandPacketType.ReportAboutEnemy, HandleReportAboutEnemy},
            };
        }

        public void OnFikaNetworkCreated(FikaNetworkManagerCreatedEvent fikaEvent)
        {
            fikaEvent.Manager.RegisterPacket<CommandPacket>(OnCommandPacketReceived);
            fikaEvent.Manager.RegisterPacket<TalkMsgPacket>(OnTalkPacketReceived);
            fikaEvent.Manager.RegisterPacket<McsBotPlayerConfigPacket>(OnMcsBotPlayerConfigPacketReceived);
        }

        public void OnCommandPacketReceived(CommandPacket packet)
        {
            if (_handleActionsMap.TryGetValue(packet.CommandType, out var action))
            {
                action(packet);
            }
        }

        public void OnTalkPacketReceived(TalkMsgPacket packet)
        {
            var fikaInstance = Singleton<IFikaNetworkManager>.Instance;
            fikaInstance.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null || !mcsLeadPlayer.IsYourPlayer)
            {
                return;
            }

            if (fikaInstance.CoopHandler.Players.TryGetValue(packet.McsBotPlayerNetId, out FikaPlayer mcsBotPlayer))
            {
                SubTitleMgr.ShowMsg(mcsLeadPlayer, mcsBotPlayer, new McsMsg
                {
                    PhraseTrigger = packet.PhraseTrigger, 
                    Position = packet.Position
                });
            }
        }

        public void OnMcsBotPlayerConfigPacketReceived(McsBotPlayerConfigPacket packet)
        {
            var fikaInstance = Singleton<IFikaNetworkManager>.Instance;
            fikaInstance.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null || !mcsLeadPlayer.IsYourPlayer)
            {
                return;
            }

            McsMgr.UpdateMcsBotPlayerConfig(mcsLeadPlayer.ProfileId, new McsBotPlayerConfig
            {
                McsLeadPlayerId = mcsLeadPlayer.ProfileId,
                PriceThreshold = packet.PriceThreshold,
                ArmorLevelThreshold = packet.ArmorLevelThreshold,
                LootingWishlishItem = packet.LootingWishlishItem,
                LootingQuestItem = packet.LootingQuestItem,
                BlockItemType = packet.BlockItemType,
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
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                botOwner.Mover.AllowTeleport();
                mcsBotPlayer.Teleport(mcsLeadPlayer.Position, true);
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
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                Vector3? validPosition = null;
                var xOffset = GClass856.Random(3f, 4f) * GClass856.RandomSing();
                var zOffset = GClass856.Random(3f, 4f) * GClass856.RandomSing();
                var newPos = packet.Position.Value + new Vector3(xOffset, 0f, zOffset);

                for (int attempt = 0; attempt < 30; attempt++)
                {
                    if (NavMesh.SamplePosition(newPos, out var navMeshHit1, 7f, -1))
                    {
                        if (Mathf.Abs(navMeshHit1.position.y - packet.Position.Value.y) <= 2f)
                        {
                            validPosition = navMeshHit1.position;
                            break;
                        }
                    }
                }

                if (validPosition == null && NavMesh.SamplePosition(newPos, out var navMeshHit2, 7f, -1))
                {
                    validPosition = navMeshHit2.position;
                }

                if (validPosition.HasValue)
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Going,
                    });
                    botOwner.GetMcsBotPlayerData().ShouldGoToPoint = true;
                    botOwner.Mover.LastTimePosChanged = Time.time;
                    botOwner.StopMove();
                    botOwner.GoToSomePointData.SetPoint(validPosition.Value);
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
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                botOwner.GetMcsBotPlayerData().ShouldHoldPosition = true;
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.HoldPosition,
                });
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
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.GetMcsBotPlayerData().ShouldGoToPoint = false;
                botOwner.GetMcsBotPlayerData().ShouldHoldPosition = false;
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
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.OnFirstContact,
                    Position = packet.Position
                });
            }
        }

        public void SendCommandPacket(CommandMgrHandleFikaEvent @event)
        {
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

        public void SendTalkPacket(SubTitleMgrHandleFikaEvent @event)
        {
            // MiyakoCarryServicePlugin.Logger.LogWarning($"尝试发送TalkPacket");
            var mcsLeadPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(@event.McsLeadPlayerId);
            var mcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(@event.McsBotPlayerId);
            if (mcsLeadPlayer == null || mcsBotPlayer == null)
            {
                // MiyakoCarryServicePlugin.Logger.LogError($"mcsLeadPlayer 或 mcsBotPlayer 其中之一为空");
                return;
            }

            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                var packet = new TalkMsgPacket
                {
                    PhraseTrigger = @event.Msg.PhraseTrigger,
                    Position = @event.Msg.Position,
                    Key = @event.Msg.Key,
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };

                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            }
        }

        public void SendMcsBotPlayerConfigPacket(ConfigEntrySettingChangedEvent @event)
        {
            var mcsLeadPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(@event.McsBotPlayerConfig.McsLeadPlayerId);
            if (mcsLeadPlayer == null)
            {
                // MiyakoCarryServicePlugin.Logger.LogError($"mcsLeadPlayer 或 mcsBotPlayer 其中之一为空");
                return;
            }
            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer)
            {
                var packet = new McsBotPlayerConfigPacket
                {
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
                    ArmorLevelThreshold = MiyakoCarryServicePlugin.ArmorLevelThreshold.Value,
                    LootingWishlishItem = MiyakoCarryServicePlugin.LootingWishlishItem.Value,
                    LootingQuestItem = MiyakoCarryServicePlugin.LootingQuestItem.Value,
                    BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value,
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}
