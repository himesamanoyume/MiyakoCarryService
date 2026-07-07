
using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using MiyakoCarryService.Fika.Packets;
using MiyakoCarryService.Fika.Utils;
using SPT.Common.Utils;
using UnityEngine;

namespace MiyakoCarryService.Fika.Mgrs
{
    public class CommandPacketMgr : BaseMgr
    {
        private LootDataMgr LootDataMgr => McsMgrApi.GetMgr<LootDataMgr>();

        public override void Start()
        {
            base.Start();

            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.Teleport.ToString(), HandleTeleport);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.GoToPoint.ToString(), HandleGoToPoint);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.HoldPosition.ToString(), HandleHoldPosition);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.Regroup.ToString(), HandleRegroup);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.ReportAboutEnemy.ToString(), HandleReportAboutEnemy);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.ReportAboutSelf.ToString(), HandleReportAboutSelf);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.OnYourOwn.ToString(), HandleOnYourOwn);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.Escort.ToString(), HandleEscort);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.GoToExfil.ToString(), HandleGoToExfil);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.AimingBodyPart.ToString(), HandleAimingBodyPart);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.QuestProxyAction.ToString(), HandleProxyAction);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.LootProxyAction.ToString(), HandleProxyAction);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.InteractionProxyAction.ToString(), HandleProxyAction);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.EndProxyAction.ToString(), HandleEndProxyAction);
            CommandPacketUtils.RegisterHandleAction(ECommandPacketType.DropTargetLoot.ToString(), HandleDropTargetLoot);
        }

        public void HandleCommandPacket(CommandPacket packet, Action<CommandPacket, FikaPlayer, FikaPlayer> action)
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

                action(packet, mcsLeadPlayer, mcsBotPlayer);
            }
        }

        public virtual void HandleTeleport(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
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

        public virtual void HandleGoToPoint(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
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
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldGoToPoint);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.TargetPos = pos.Value;
                    mcsBotPlayerData.ProxyTargetId = null;
                }
                botOwner.Mover.LastTimePosChanged = Time.time;
                botOwner.StopMove();
            }
        }

        public virtual void HandleEscort(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
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
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldEscort);
                    mcsBotPlayerData.TargetPos = packet.Position.Value;
                    mcsBotPlayerData.IsLooting = false;
                }
            }
        }

        public virtual void HandleAimingBodyPart(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.AimingBodyPartType = packet.AimingBodyPartType;
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });
            }
        }

        public virtual void HandleGoToExfil(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.TargetPos = null;
                mcsBotPlayerData.IsLooting = false;
                mcsBotPlayerData.SetDecision(null, Decisions.ShouldExfil);
            }
        }

        public virtual void HandleHoldPosition(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            botOwner.StopMove();
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldHoldPosition);
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.HoldPosition,
                });
            }
        }

        public virtual void HandleDropTargetLoot(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            if (!botOwner.ExternalItemsController.HaveItemsToDrop())
            {
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Negative,
                });
                return;
            }

            botOwner.StopMove();
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldDropTargetLoot);
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });
            }
        }

        public virtual void HandleRegroup(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.SetDecision(null, Decisions.ShouldRegroup);
                mcsBotPlayerData.IsLooting = false;
                mcsBotPlayerData.TargetPos = null;
            }
            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Regroup,
            });
        }

        public virtual void HandleReportAboutEnemy(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
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
            else
            {
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Clear,
                });
            }
        }

        public virtual void HandleReportAboutSelf(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var health = botOwner.HealthController.GetBodyPartHealth(EBodyPart.Common);
            var key1 = $"{(int)health.Current}/{health.Maximum}";
            botOwner.CollectAmmoOrBackupAmmoCount(out var total);
            var key2 = total.ToString();
            var allActiveEffects = botOwner.HealthController.GetAllActiveEffects();
            var healthStates = new List<HealthState>();
            foreach (var activeEffect in allActiveEffects)
            {
                
                if (Classification.EffectTypeFilter.Contains(activeEffect.Type))
                {
                    continue;
                }

                var effectType = GClass3058.EffectName(activeEffect);
                if (string.IsNullOrEmpty(effectType))
                {
                    continue;
                }

                healthStates.Add(new HealthState
                {
                    BodyPart = activeEffect.BodyPart.ToString(),
                    EffectType = effectType
                });
            }
            var key3 = Json.Serialize(healthStates);
            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Mine,
                Keys = [key1, key2, key3]
            });
        }

        public virtual void HandleOnYourOwn(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.SetDecision();
                mcsBotPlayerData.IsLooting = false;
                mcsBotPlayerData.TargetPos = null;
            }
            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Roger,
            });
        }

        public virtual void HandleProxyAction(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                if (packet.CommandType == ECommandPacketType.QuestProxyAction.ToString())
                {
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldQuestProxyAction);
                    mcsBotPlayerData.ProxyTargetId = packet.TargetId;
                    mcsBotPlayerData.TargetPos = packet.Position;
                    mcsBotPlayerData.IsLooting = false;
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Roger,
                    });
                }
                else if (packet.CommandType == ECommandPacketType.LootProxyAction.ToString())
                {
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldLootProxyAction);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.ProxyTargetId = packet.TargetId;
                    var lootData = LootDataMgr.FindLootData(packet.TargetId);
                    mcsBotPlayerData.TargetPos = lootData.RootTransform.position;
                    LootDataMgr.UnlockLootingTarget(lootData);
                    LootDataMgr.UnlockLootingTargetRootTransform(lootData.RootTransform);
                    if (!LootDataMgr.IsLockedLootingTarget(lootData) && !LootDataMgr.IsLockedLootingTargetRootTransform(lootData.RootTransform))
                    {
                        LootDataMgr.LockLootItemToTarget(lootData);
                        LootDataMgr.LockLootingTargetRootTransform(lootData.RootTransform);
                        mcsBotPlayerData.LootingTarget = lootData;
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Roger,
                        });
                    }
                    else
                    {
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Negative,
                        });
                        mcsBotPlayerData.RemoveDecision(Decisions.ShouldLootProxyAction);
                        mcsBotPlayerData.ProxyTargetId = null;
                        mcsBotPlayerData.TargetPos = null;
                    }
                }
                else if (packet.CommandType == ECommandPacketType.InteractionProxyAction.ToString())
                {
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldInteractionProxyAction);
                    mcsBotPlayerData.ProxyTargetId = packet.TargetId;
                    var interactableObjectData = Singleton<GameWorld>.Instance.FindInteractableObjectData(packet.TargetId);
                    if (interactableObjectData != null)
                    {
                        mcsBotPlayerData.ProxyTargetId = interactableObjectData.Id();
                        mcsBotPlayerData.TargetPos = interactableObjectData.GetPos();
                        mcsBotPlayerData.IsLooting = false;
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Roger,
                        });
                    }
                    else
                    {
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Negative,
                        });
                        mcsBotPlayerData.RemoveDecision(Decisions.ShouldInteractionProxyAction);
                        mcsBotPlayerData.ProxyTargetId = null;
                        mcsBotPlayerData.TargetPos = null;
                    }
                }
            }
        }

        public virtual void HandleEndProxyAction(CommandPacket packet, FikaPlayer mcsLeadPlayer, FikaPlayer mcsBotPlayer)
        {
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.RemoveDecision([Decisions.ShouldInteractionProxyAction, Decisions.ShouldQuestProxyAction, Decisions.ShouldLootProxyAction, Decisions.ShouldHoldPosition]);
                mcsBotPlayerData.TargetPos = null;
                mcsBotPlayerData.ProxyTargetId = null;
            }
        }
    }
}