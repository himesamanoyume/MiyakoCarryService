using System.Collections.Generic;
using MiyakoCarryService.Client;
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

namespace MiyakoCarryService.Fika
{
    public class MiyakoCarryServiceFika
    {
        private Dictionary<ECommandPacketType, Action<CommandPacket>> _handleActionsMap;

        private CommandMgr CommandMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<CommandMgr>();
            }
        }

        private SubTitleMgr SubTitleMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SubTitleMgr>();
            }
        }

        public void InitMcsFika()
        {
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
            _handleActionsMap = new()
            {
                {ECommandPacketType.Teleport, HandleTeleport},
                {ECommandPacketType.GoToPoint, HandleGoToPoint},
                {ECommandPacketType.HoldPosition, HandleHoldPosition},
                {ECommandPacketType.Regroup, HandleRegroup},
            };
        }

        public void OnFikaNetworkCreated(FikaNetworkManagerCreatedEvent fikaEvent)
        {
            fikaEvent.Manager.RegisterPacket<CommandPacket>(OnCommandPacketReceived);
            fikaEvent.Manager.RegisterPacket<TalkMsgPacket>(OnTalkPacketReceived);

            SubTitleMgr.HandleFikaEvent = SendTalkPacket;
            CommandMgr.HandleFikaEvent = SendCommandPacket;
            // CommandMgr.HandleFikaEventAction.TryAdd(ECommandPacketType.Teleport, SendTeleportCommandPacket);
            // CommandMgr.HandleFikaEventAction.TryAdd(ECommandPacketType.GoToPoint, );
            // CommandMgr.HandleFikaEventAction.TryAdd(ECommandPacketType.HoldPosition, );
            // CommandMgr.HandleFikaEventAction.TryAdd(ECommandPacketType.Regroup, );
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
            // MiyakoCarryServicePlugin.Logger.LogWarning($"接收到 TalkPacket");

            var fikaInstance = Singleton<IFikaNetworkManager>.Instance;

            fikaInstance.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null || !mcsLeadPlayer.IsYourPlayer)
            {
                return;
            }

            if (fikaInstance.CoopHandler.Players.TryGetValue(packet.McsBotPlayerNetId, out FikaPlayer mcsBotPlayer))
            {
                SubTitleMgr.ShowMsg(mcsLeadPlayer.Profile, mcsBotPlayer.Profile, packet.PhraseTrigger, packet.Position);
            }
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
                botOwner.TalkMsg(EPhraseTrigger.Roger);
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
                    botOwner.TalkMsg(EPhraseTrigger.Going);
                    botOwner.GetMcsBotData().ShouldGoToPoint = true;
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
                botOwner.GetMcsBotData().ShouldHoldPosition = true;
                botOwner.TalkMsg(EPhraseTrigger.HoldPosition);
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
                botOwner.GetMcsBotData().ShouldGoToPoint = false;
                botOwner.GetMcsBotData().ShouldHoldPosition = false;
                botOwner.TalkMsg(EPhraseTrigger.Regroup);
            }
        }

        public void SendCommandPacket(Player mcsBotPlayer, ECommandPacketType commandPacketType, Vector3? position)
        {
            var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                var packet = new CommandPacket(commandPacketType)
                {
                    Position = position,
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        public void SendTalkPacket(MongoID mcsLeadPlayerId, MongoID mcsBotPlayerId, EPhraseTrigger phraseTrigger, Vector3? position)
        {
            // MiyakoCarryServicePlugin.Logger.LogWarning($"尝试发送TalkPacket");
            var mcsLeadPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(mcsLeadPlayerId);
            var mcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(mcsBotPlayerId);
            if (mcsLeadPlayer == null || mcsBotPlayer == null)
            {
                MiyakoCarryServicePlugin.Logger.LogError($"mcsLeadPlayer 或 mcsBotPlayer 其中之一为空");
                return;
            }

            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                var packet = new TalkMsgPacket(phraseTrigger)
                {
                    Position = position,
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };

                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }
}
