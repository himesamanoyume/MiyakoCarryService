
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public sealed class GoToLootTargetLogic : McsBotBaseLogic
    {
        private int _currentLootingRetries = 0;
        private float _lastTimeCheckDistance = 0f;
        public GoToLootTargetLogic(BotOwner botOwner) : base(botOwner)
        {

        }

        public override void Start()
        {
            base.Start();
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();

            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.IsLooting = true;
                BotOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.GoLoot
                });
            }
        }

        public override void Stop()
        {
            base.Stop();
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.IsLooting = false;
            }
        }

        private DoorInteractionStatus HandleDoor()
        {
            return BotOwner.DoorOpener.UpdateDoorInteractionStatus();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            HandleDoor();
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
                var distance = BotOwner.Position.McsSqrDistance(lootPos);

#if DEBUG
                // MiyakoCarryServicePlugin.Logger.LogWarning($"{mcsBotPlayerData.Player.Profile.Nickname}, 目标: {mcsBotPlayerData.LootingTarget.Item.Name.McsLocalized()}, 价值: {mcsBotPlayerData.LootingTarget.Offer.Price}, 坐标: {targetPos}, Sqr距离: {distance}, 高度差: {Math.Abs(offset.y)}");
#endif

                if (distance <= 9f && Math.Abs(offset.y) < 2f)
                {
                    BotOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.OnLoot,
                        Key = mcsBotPlayerData.LootingTarget.Item.Name,
                        Key2 = $"{mcsBotPlayerData.LootingTarget.Offer.Price} {mcsBotPlayerData.LootingTarget.Offer.CurrencySignal}"
                    });
                    BotOwner.SetTargetMoveSpeed(0f);
                    BotOwner.SetPose(0f);
                    BotOwner.Steering.LookToPoint(lootPos);
                    TasksExtensions.HandleExceptions(StartLooting());
                    return;
                }

                if (distance <= 5f)
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

        private async Task Execute(McsBotPlayerData mcsBotPlayerData, GInterface424 action, LootData targetLootData)
        {
            var mcsBotPlayer = mcsBotPlayerData.Player;
            var callback = new Callback((IResult result) =>
            {
                if (result.Succeed)
                {
                    mcsBotPlayer.UpdateInteractionCast();

                    // // 可选触发OnItemTaken事件 
                    // if (BotOwner.ItemTaker != null)
                    // {
                    //     // 当前无需要触发的事件
                    // }
                }

                mcsBotPlayer.CurrentManagedState.Pickup(false, null);
            });

            mcsBotPlayer.CurrentManagedState.Pickup(true, new Action(() =>
            {
                if (action is GClass3411 gClass3411)
                {
                    if (targetLootData.Item is MagazineItemClass magazineItemClass && magazineItemClass != null)
                    {
                        mcsBotPlayer.InventoryController.StrictCheckMagazine(magazineItemClass, false, 0, false, true);
                    }

                    if (targetLootData.Item is ContainerClass containerClass && containerClass != null)
                    {
                        foreach (var mag in containerClass.GetAllItemsFromCollection().OfType<MagazineItemClass>())
                        {
                            mcsBotPlayer.InventoryController.StrictCheckMagazine(mag, false, 0, false, true);
                        }
                    }
                    BotOwner.AITaskManager.RegisterDelayedTask(BotOwner, 0.5f, new Action(BotOwner.Medecine.RefreshCurMeds));
                }

                mcsBotPlayer.InventoryController.RunNetworkTransaction(action, callback);
            }));
        }

        private async Task<bool> HandleLootAction(McsBotPlayerData mcsBotPlayerData, LootData targetLootData)
        {
            if (!targetLootData.LootProps.TryGetValue(mcsBotPlayerData.McsAILeadPlayer, out var lootProp))
            {
                return false;
            }

            var mcsBotPlayerInventoryController = mcsBotPlayerData.Player.InventoryController;
            var normalTake = false;
            var currentSlot = targetLootData.ItemType == EItemType.Backpack ? mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack) :
                    targetLootData.ItemType == EItemType.Equipment ? mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

            if (currentSlot == null)
            {
                normalTake = true;
            }

            if (normalTake)
            {
#if DEBUG
                // MiyakoCarryServicePlugin.Logger.LogWarning("触发拿取战利品1");
#endif
                await InteractionDelay(targetLootData);
                return await Take(mcsBotPlayerData, targetLootData);
            }
            else
            {
                if (lootProp.IsShouldEquipContainer(mcsBotPlayerData.BotOwner))
                {
#if DEBUG
                    // MiyakoCarryServicePlugin.Logger.LogWarning("触发装备战利品");
#endif
                    return await Equip(mcsBotPlayerData, targetLootData);
                }
                else if (lootProp.IsShouldNestContainer(mcsBotPlayerData.BotOwner) is ENestType.In or ENestType.Out)
                {
#if DEBUG
                    // MiyakoCarryServicePlugin.Logger.LogWarning("触发嵌套战利品");
#endif
                    return await Nest(mcsBotPlayerData, targetLootData, lootProp.IsShouldNestContainer(mcsBotPlayerData.BotOwner));
                }
                else if (lootProp.IsShouldSwapContainer(mcsBotPlayerData.BotOwner))
                {
#if DEBUG
                    // MiyakoCarryServicePlugin.Logger.LogWarning("触发交换战利品");
#endif
                    return await Swap(mcsBotPlayerData, targetLootData);
                }
                else if (lootProp.IsShouldTakeContainer(mcsBotPlayerData.BotOwner))
                {
#if DEBUG
                    // MiyakoCarryServicePlugin.Logger.LogWarning("触发拿取战利品2");
#endif
                    await InteractionDelay(targetLootData);
                    return await Take(mcsBotPlayerData, targetLootData);
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task<bool> Swap(McsBotPlayerData mcsBotPlayerData, LootData targetLootData)
        {
            var mcsBotPlayerInventoryController = mcsBotPlayerData.Player.InventoryController;
            var currentSlot = targetLootData.ItemType == EItemType.Backpack ? mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack) :
                    targetLootData.ItemType == EItemType.Equipment ? mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

            if (currentSlot == null)
            {
                return false;
            }

            var currentContainer = currentSlot.ContainedItem;
            if (currentContainer == null)
            {
                return false;
            }

            var currentItemData = currentContainer.GetData();
            if (currentItemData == null || currentItemData is not LootData currentLootData)
            {
                return false;
            }

            if (targetLootData.ItemType == EItemType.Equipment || (targetLootData.ItemType == EItemType.Backpack && !currentLootData.IsContainerWithAdditionalGrid))
            {
                await Throw(mcsBotPlayerData, currentLootData, targetLootData);
                await InteractionDelay(3);
                await Equip(mcsBotPlayerData, targetLootData);
                return true;
            }

            return false;
        }

        private async Task<bool> Equip(McsBotPlayerData mcsBotPlayerData, LootData targetLootData)
        {
            var mcsBotPlayerInventoryController = mcsBotPlayerData.Player.InventoryController;
            var toAddress = mcsBotPlayerInventoryController.FindSlotToPickUp(targetLootData.Item);

            if (toAddress != null)
            {
                var result = InteractionsHandlerClass.Move(
                    targetLootData.Item,
                    toAddress,
                    mcsBotPlayerInventoryController,
                    true
                );

                if (result.Succeeded)
                {
                    await Execute(mcsBotPlayerData, result.Value, targetLootData);
                    await Sort(mcsBotPlayerInventoryController);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 丢东西之前需要先将内部的物品进行转移
        /// </summary>
        private async Task Throw(McsBotPlayerData mcsBotPlayerData, LootData throwLootData, LootData retainLootData)
        {
            await Transfer(mcsBotPlayerData, throwLootData, retainLootData);
            mcsBotPlayerData.Player.InventoryController.ThrowItem(throwLootData.Item);
        }

        /// <summary>
        /// 转移原容器中最顶层的物品至新容器
        /// </summary>
        private async Task Transfer(McsBotPlayerData mcsBotPlayerData, LootData fromLootData, LootData toLootData)
        {
            if (fromLootData.Item is ContainerClass containerClass)
            {
                var firstLevelItems = containerClass.GetFirstLevelItems().ToList();
                foreach (var firstLevelItem in firstLevelItems)
                {
                    var firstLevelItemData = firstLevelItem.GetData();
                    if (firstLevelItem == null)
                    {
                        continue;
                    }

                    if (firstLevelItemData is LootData firstLevelLootData)
                    {
                        await Take(mcsBotPlayerData, firstLevelLootData, toLootData);
                        await InteractionDelay(1);
                    }
                }
            }
        }

        private async Task<bool> Nest(McsBotPlayerData mcsBotPlayerData, LootData targetLootData, ENestType nestType)
        {
            var mcsBotPlayerInventoryController = mcsBotPlayerData.Player.InventoryController;
            var currentSlot = targetLootData.ItemType == EItemType.Backpack ? mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack) :
                    targetLootData.ItemType == EItemType.Equipment ? mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

            if (currentSlot == null)
            {
                return false;
            }

            var currentContainer = currentSlot.ContainedItem;
            if (currentContainer == null)
            {
                return false;
            }

            var currentItemData = currentContainer.GetData();
            if (currentItemData == null || currentItemData is not LootData currentLootData)
            {
                return false;
            }

            if (nestType == ENestType.Out)
            {
                var result = await Take(mcsBotPlayerData, currentLootData, targetLootData);
                await InteractionDelay(1);
                return result && await Equip(mcsBotPlayerData, targetLootData);
            }
            else if (nestType == ENestType.In)
            {
                await Transfer(mcsBotPlayerData, currentLootData, targetLootData);
                return await Take(mcsBotPlayerData, targetLootData, currentLootData);
            }

            return false;
        }

        /// <summary>
        /// 拾取不再只是单纯往胸挂、背包、口袋槽位内拾取，而是包括胸挂、背包、口袋本身以外还有其中嵌套的容器，全部都要进行尝试放入
        /// </summary>
        private async Task<bool> Take(McsBotPlayerData mcsBotPlayerData, LootData fromLootData, LootData toLootData = null)
        {
            var mcsBotPlayerInventoryController = mcsBotPlayerData.Player.InventoryController;
            var targets = new List<CompoundItem>();

            if (toLootData != null)
            {
                if (toLootData.Item is CompoundItem compoundItem)
                {
                    targets.Add(compoundItem);
                }
            }
            else
            {
                var allContainers = new List<Item>();
                var pocketsSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Pockets);
                if (pocketsSlot.ContainedItem != null)
                {
                    pocketsSlot.ContainedItem.GetAllItemsNonAlloc(allContainers, false, true);
                }

                var tacticalVestSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
                if (tacticalVestSlot.ContainedItem != null)
                {
                    tacticalVestSlot.ContainedItem.GetAllItemsNonAlloc(allContainers, false, true);
                }

                var backpckSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
                if (backpckSlot.ContainedItem != null)
                {
                    backpckSlot.ContainedItem.GetAllItemsNonAlloc(allContainers, false, true);
                }

                allContainers = allContainers.Where(i => i.IsContainer).ToList();

                foreach (var container in allContainers)
                {
                    if (container is CompoundItem compoundItem)
                    {
                        targets.Add(compoundItem);
                    }
                }
            }

            var result = InteractionsHandlerClass.QuickFindAppropriatePlace(
                fromLootData.Item,
                mcsBotPlayerInventoryController,
                targets,
                InteractionsHandlerClass.EMoveItemOrder.PickUp,
                true
            );

            if (result.Succeeded)
            {
                await Execute(mcsBotPlayerData, result.Value, fromLootData);
                await Sort(mcsBotPlayerInventoryController);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 对包括背包、胸挂及其内部的容器全部进行整理，确保空间利用最大化
        /// </summary>
        private async Task Sort(InventoryController mcsBotPlayerInventoryController, List<Item> allContainers = null)
        {
            await InteractionDelay(2);

            if (allContainers == null)
            {
                allContainers = new();

                var tacticalVestSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
                if (tacticalVestSlot.ContainedItem != null)
                {
                    tacticalVestSlot.ContainedItem.GetAllItemsNonAlloc(allContainers, false, false);
                }

                var backpckSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
                if (backpckSlot.ContainedItem != null)
                {
                    backpckSlot.ContainedItem.GetAllItemsNonAlloc(allContainers, false, false);
                }

                allContainers = allContainers.Where(i => i.IsContainer).ToList();
            }

            foreach (var item in allContainers)
            {
                if (item is SearchableItemItemClass searchableItemItemClass)
                {
                    var sortResult = InteractionsHandlerClass.Sort(searchableItemItemClass, mcsBotPlayerInventoryController, true);
                    if (sortResult.Succeeded)
                    {
                        await mcsBotPlayerInventoryController.TryRunNetworkTransaction(sortResult, null);
                    }
                }
            }
        }

        private async Task InteractionDelay(LootData lootData)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(lootData.Item.ExamineTime * 300f));
        }

        private async Task InteractionDelay(float time)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(time * 1000f));
        }
    }
}