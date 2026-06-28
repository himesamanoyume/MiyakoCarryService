
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
                // mcsBotPlayerData.IsLooting = true;
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger
                });
            }
        }

        public override void Stop()
        {
            base.Stop();
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                // mcsBotPlayerData.IsLooting = false;
            }
        }

        private DoorInteractionStatus HandleDoor()
        {
            return BotOwner.DoorOpener.UpdateDoorInteractionStatus();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            HandleDoor();
            _baseLogic.UpdateNodeByMain(data);
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();

            if (mcsBotPlayerData == null)
            {
                return;
            }

            if (!mcsBotPlayerData.IsLooting)
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

                if (!mcsBotPlayerData.LootingTarget.IsLocked())
                {
                    mcsBotPlayerData.IsLooting = false;
                    return;
                }

                var lootPos = mcsBotPlayerData.LootingTarget.RootTransform.position;
                var offset = BotOwner.Position - lootPos;
                var sqrDistance = BotOwner.Position.McsSqrDistance(lootPos);

                if (sqrDistance <= 4f && Math.Abs(offset.y) < 2f)
                {
                    BotOwner.SetTargetMoveSpeed(0f);
                    BotOwner.SetPose(0f);
                    BotOwner.Steering.LookToPoint(lootPos);

                    if (mcsBotPlayerData.HasDecision([EDecision.ShouldInteractionProxyAction, EDecision.ShouldQuestProxyAction, EDecision.ShouldLootProxyAction]))
                    {
                        mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup, EDecision.ShouldInteractionProxyAction, EDecision.ShouldQuestProxyAction, EDecision.ShouldLootProxyAction], EDecision.ShouldHoldPosition);
                        BotOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.OnPosition
                        });
                        if (mcsBotPlayerData.HasDecision(EDecision.ShouldQuestProxyAction))
                        {
                            EventMgr.Notify(new QuestProxyActionReadyToStartEvent
                            {
                                McsLeadPlayerId = mcsBotPlayerData.LeadPlayer.ProfileId,
                                McsBotPlayerId = mcsBotPlayerData.Player.ProfileId,
                                TargetId = mcsBotPlayerData.ProxyTargetId
                            });
                        }
                        else if (mcsBotPlayerData.HasDecision(EDecision.ShouldLootProxyAction))
                        {
                            TasksExtensions.HandleExceptions(StartLooting());
                        }
                        else if (mcsBotPlayerData.HasDecision(EDecision.ShouldInteractionProxyAction))
                        {
                            var interactableObjectData = Singleton<GameWorld>.Instance.FindInteractableObjectData(mcsBotPlayerData.ProxyTargetId);
                            if (interactableObjectData != null)
                            {
                                var interactionResult = new InteractionResult(EInteractionType.Open);
                                mcsBotPlayerData.Player.CurrentManagedState.StartDoorInteraction(interactableObjectData.GetWorldInteractiveObject(), interactionResult, null);
                            }
                        }
                    }
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
                }
            }
        }
    }
}