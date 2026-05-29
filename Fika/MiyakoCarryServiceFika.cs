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
using MiyakoCarryService.Fika.Patches;

namespace MiyakoCarryService.Fika
{
    public class MiyakoCarryServiceFika
    {
        private Dictionary<ECommandPacketType, Action<CommandPacket>> _handleActionsMap;
        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        private SubtitlesMgr SubtitlesMgr => MgrAccessor.Get<SubtitlesMgr>();

        public void InitMcsFika()
        {
            new ExtractPatch().Enable();

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
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                Vector3? validPosition = null;
                var xOffset = GClass856.Random(3f, 4f) * GClass856.RandomSing();
                var zOffset = GClass856.Random(3f, 4f) * GClass856.RandomSing();
                var newPos = packet.Position.Value + new Vector3(xOffset, 0f, zOffset);

                for (int attempt = 0; attempt < 30; attempt++)
                {
                    if (Tools.BetterDestination(7f, newPos, out var targetPos))
                    {
                        if (Mathf.Abs(targetPos.y - packet.Position.Value.y) <= 2f)
                        {
                            validPosition = targetPos;
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
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                mcsBotPlayerData.ShouldGoToPoint = false;
                mcsBotPlayerData.ShouldHoldPosition = false;
                mcsBotPlayerData.IsLooting = false;
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
                if (botOwner.Memory.HaveEnemy)
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.OnFirstContact,
                        Position = botOwner.Memory.GoalEnemy.EnemyLastPosition
                    });
                }
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
