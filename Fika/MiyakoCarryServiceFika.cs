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
            };
        }

        public void OnFikaNetworkCreated(FikaNetworkManagerCreatedEvent fikaEvent)
        {
            fikaEvent.Manager.RegisterPacket<CommandPacket, NetPeer>(OnCommandPacketReceived);
            fikaEvent.Manager.RegisterPacket<TalkPacket, NetPeer>(OnTalkPacketReceived);
            if (FikaBackendUtils.IsServer)
            {
                SubTitleMgr.HandleFikaEvent = SendTalkPacket;
            }
            else
            {
                CommandMgr.HandleFikaEventsMap.TryAdd(ECommandPacketType.Teleport, SendTeleportCommandPacket);
            }
        }

        public void OnCommandPacketReceived(CommandPacket packet, NetPeer netPeer)  
        {  
            if (_handleActionsMap.TryGetValue(packet.CommandType, out var action))
            {
                action(packet);
            }
        }
        
        public void OnTalkPacketReceived(TalkPacket packet, NetPeer netPeer)
        {
            var fikaInstance = Singleton<IFikaNetworkManager>.Instance;

            fikaInstance.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null || !mcsLeadPlayer.IsYourPlayer)
            {
                return;
            }

            if (fikaInstance.CoopHandler.Players.TryGetValue(packet.McsBotPlayerNetId, out FikaPlayer mcsBotPlayer))
            {
                SubTitleMgr.ShowMcsBotPlayerMsg(mcsBotPlayer.ProfileId, packet.TalkContentType);
            }
        }

        private void HandleTeleport(CommandPacket packet)
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
                mcsBotPlayer.Teleport(mcsLeadPlayer.Position);
            }
        }

        public void SendTeleportCommandPacket(Player mcsBotPlayer, Vector3 position)
        {
            var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                var packet = new CommandPacket(ECommandPacketType.Teleport)
                {
                    Position = position,
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        public void SendTalkPacket(Player mcsBotPlayer, ETalkContentType talkContentType, Vector3 position)
        {
            var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                var netPeer = FindPeerByNetId(fikaMcsLeadPlayer.NetId);
                if (netPeer == null)  
                {  
                    MiyakoCarryServicePlugin.Logger.LogError($"未找到NetId {fikaMcsLeadPlayer.NetId} 的NetPeer");
                    return;  
                }
                
                var packet = new TalkPacket(talkContentType)
                {
                    Position = position,
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };

                Singleton<IFikaNetworkManager>.Instance.SendDataToPeer(ref packet, DeliveryMethod.ReliableOrdered, netPeer);
            }
        }

        private NetPeer FindPeerByNetId(int netId)  
        {  
            var server = Singleton<FikaServer>.Instance;  
            if (server == null || server.NetServer == null)
            {
                return null;  
            }
            
            foreach (NetPeer peer in server.NetServer.ConnectedPeerList)  
            {  
                if (peer.RemoteId == netId)  
                {  
                    return peer;  
                }  
            }  
            return null;  
        }
    }
}
