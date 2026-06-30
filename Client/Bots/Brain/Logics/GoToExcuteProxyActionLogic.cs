
using System;
using System.Threading.Tasks;
using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class GoToExcuteProxyActionLogic : McsBotBaseLogic
    {
        private GoToPointBaseLogic _baseLogic;
        private int _currentLootingRetries = 0;
        private float _lastTimeCheckDistance = 0f;
        public GoToExcuteProxyActionLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            base.Start();
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();

            if (mcsBotPlayerData != null)
            {
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger
                });
            }
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
            BotOwner.SetTargetMoveSpeed(1f);
            BotOwner.Sprint(true, false);
            BotOwner.SetPose(1f);
            BotOwner.Steering.LookToMovingDirection();

            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }

            if (mcsBotPlayerData.IsTaskRunning)
            {
                return;
            }

            if (_lastTimeCheckDistance < Time.time)
            {
                _currentLootingRetries++;
                if (_currentLootingRetries > 15)
                {
                    mcsBotPlayerData.IsLooting = false;
                    _currentLootingRetries = 0;
                    return;
                }

                _lastTimeCheckDistance = Time.time + 2f;

                var targetPos = mcsBotPlayerData.TargetPos;
                var offset = BotOwner.Position - targetPos.Value;
                var sqrDistance = BotOwner.Position.McsSqrDistance(targetPos.Value);

                if (sqrDistance <= 4f && Math.Abs(offset.y) < 2f)
                {
                    BotOwner.SetTargetMoveSpeed(0f);
                    BotOwner.Steering.LookToPoint(targetPos.Value);
                    TasksExtensions.HandleExceptions(StartExcuteProxyAction());
                    return;
                }

                if (sqrDistance <= 25f)
                {
                    BotOwner.SetTargetMoveSpeed(1f);
                    BotOwner.Steering.LookToMovingDirection();
                    BotOwner.Mover.Sprint(false);
                }
            }
        }

        private async Task StartExcuteProxyAction()
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            try
            {
                if (mcsBotPlayerData == null)
                {
                    return;
                }

                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.OnPosition
                });

                mcsBotPlayerData.IsTaskRunning = true;
                if (mcsBotPlayerData.HasDecision(EDecision.ShouldQuestProxyAction))
                {
                    BotOwner.SetPose(0f);
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldHoldPosition);
                    await QuestProxyActionReadyToStart();
                }
                else if (mcsBotPlayerData.HasDecision(EDecision.ShouldLootProxyAction))
                {
                    BotOwner.SetPose(0f);
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldHoldPosition);
                    await StartLooting();
                }
                else if (mcsBotPlayerData.HasDecision(EDecision.ShouldInteractionProxyAction))
                {
                    BotOwner.SetPose(1f);
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldHoldPosition);
                    var interactableObjectData = Singleton<GameWorld>.Instance.FindInteractableObjectData(mcsBotPlayerData.ProxyTargetId);
                    if (interactableObjectData == null)
                    {
                        InteractionCallback(mcsBotPlayerData);
                        return;
                    }

                    var interactionResult = new InteractionResult(EInteractionType.Open);
                    if (interactableObjectData is DoorData doorData)
                    {
                        doorData.Door.DoorState = EDoorState.Shut;
                        mcsBotPlayerData.Player.vmethod_0(doorData.Door, interactionResult, () => InteractionCallback(mcsBotPlayerData));
                    }
                    else if (interactableObjectData is SwitchData switchData)
                    {
                        mcsBotPlayerData.Player.vmethod_1(switchData.Switch, interactionResult);
                        await Task.Delay(1000);
                        InteractionCallback(mcsBotPlayerData);
                    }
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
            }
            finally
            {
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.IsTaskRunning = false;
                }
            }
        }

        private void InteractionCallback(McsBotPlayerData mcsBotPlayerData)
        {
            mcsBotPlayerData.RemoveDecision([EDecision.ShouldInteractionProxyAction, EDecision.ShouldQuestProxyAction, EDecision.ShouldLootProxyAction, EDecision.ShouldHoldPosition]);
            mcsBotPlayerData.TargetPos = null;
            mcsBotPlayerData.ProxyTargetId = null;
        }

        private async Task QuestProxyActionReadyToStart()
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }
            var isMySquadMember = CommandMgr.IsMcsMemberPlayer(mcsBotPlayerData.Player.ProfileId);
            var mcsBotPlayer = mcsBotPlayerData.Player;
            if (mcsBotPlayer == null)
            {
                return;
            }

            if (MiyakoCarryServicePlugin.FikaInstalled && !isMySquadMember)
            {
                EventMgr.Notify(new QuestProxyCommandCallbackHandleFikaEvent
                {
                    McsLeadPlayerId = mcsBotPlayerData.LeadPlayer.ProfileId,
                    McsBotPlayerId = mcsBotPlayer.ProfileId,
                    TargetId = mcsBotPlayerData.ProxyTargetId
                });
            }
            else
            {
                var questData = QuestDataMgr.FindQuestData(mcsBotPlayerData.ProxyTargetId);
                if (questData != null)
                {
                    await questData.ForceCompleteQuest(mcsBotPlayer);
                }
            }
        }

        private async Task StartLooting()
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            try
            {
                if (mcsBotPlayerData == null)
                {
                    return;
                }

                if (mcsBotPlayerData.LootingTarget == null)
                {
                    return;
                }

                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.OnLoot,
                    Key = mcsBotPlayerData.LootingTarget.Item.Name,
                    Key2 = $"{mcsBotPlayerData.LootingTarget.Offer.Price} {mcsBotPlayerData.LootingTarget.Offer.CurrencySignal}"
                });

                mcsBotPlayerData.IsTaskRunning = true;
                await Task.Delay(1000);
                if (!mcsBotPlayerData.LootingTarget.IsLocked())
                {
                    return;
                }

                var player = BotOwner.GetPlayer;
                var inventoryController = player.InventoryController;
                var item = mcsBotPlayerData.LootingTarget?.Item;

                if (item == null || item.Parent == null)
                {
                    return;
                }

                var rootItem = item.GetRootItem();
                if (rootItem == null || rootItem.Owner == null)
                {
                    return;
                }

                var rootItemData = rootItem.GetData();
                if (rootItemData is PlayerData playerData && playerData.Player.HealthController.IsAlive)
                {
                    return;
                }

                if (!Singleton<GameWorld>.Instance.ItemOwners.TryGetValue(rootItem.Owner, out var itemOwner))
                {
                    return;
                }

                var lootableContainer = itemOwner.Transform.GetComponent<LootableContainer>();
                var isActuallyInContainer = lootableContainer != null;

                if (isActuallyInContainer)
                {
                    if (lootableContainer.DoorState == EDoorState.Shut || lootableContainer.DoorState == EDoorState.Locked)
                    {
                        var interactionResult = new InteractionResult(EInteractionType.Open);
                        player.CurrentManagedState.StartDoorInteraction(lootableContainer, interactionResult, null);

                        await Task.Delay(2500);

                        if (lootableContainer.DoorState < EDoorState.Open)
                        {
                            BotOwner.TalkMsg(new McsMsg
                            {
                                PhraseTrigger = EPhraseTrigger.PhraseNone,
                                Key = Locales.ONLOOTOPENCONTAINERFAILED
                            });
                            return;
                        }
                    }

                    await Task.Delay(3000);
                }

                if (item.QuestItem)
                {
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.PhraseNone,
                        Key = Locales.ONLOOTQUESTITEM
                    });
                    return;
                }

                if (await HandleLootAction(mcsBotPlayerData, mcsBotPlayerData.LootingTarget))
                {
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.LootGeneric,
                        Key = item.Name
                    });
                }
                else
                {
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.PhraseNone,
                        Key = Locales.ONLOOTNOSPACE
                    });
                }

                if (isActuallyInContainer)
                {
                    await Task.Delay(2000);
                    var interactionResult2 = new InteractionResult(EInteractionType.Close);
                    lootableContainer.Interact(interactionResult2);
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
            }
            finally
            {
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.IsTaskRunning = false;
                    InteractionCallback(mcsBotPlayerData);
                }
            }
        }
    }
}