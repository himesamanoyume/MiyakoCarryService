
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Enums;
using System.Threading.Tasks;
using MiyakoCarryService.Client.Datas;
using Comfort.Common;
using System;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public abstract class McsBotBaseLogic(BotOwner botOwner) : CustomLogic(botOwner)
    {
        protected async Task Execute(McsBotPlayerData mcsBotPlayerData, GInterface424 action, LootData targetLootData)
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

        protected async Task<bool> HandleLootAction(McsBotPlayerData mcsBotPlayerData, LootData targetLootData)
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

        protected async Task<bool> Swap(McsBotPlayerData mcsBotPlayerData, LootData targetLootData)
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

        protected async Task<bool> Equip(McsBotPlayerData mcsBotPlayerData, LootData targetLootData)
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
        protected async Task Throw(McsBotPlayerData mcsBotPlayerData, LootData throwLootData, LootData retainLootData)
        {
            await Transfer(mcsBotPlayerData, throwLootData, retainLootData);
            mcsBotPlayerData.Player.InventoryController.ThrowItem(throwLootData.Item);
        }

        /// <summary>
        /// 转移原容器中最顶层的物品至新容器
        /// </summary>
        protected async Task Transfer(McsBotPlayerData mcsBotPlayerData, LootData fromLootData, LootData toLootData)
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

        protected async Task<bool> Nest(McsBotPlayerData mcsBotPlayerData, LootData targetLootData, ENestType nestType)
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
        protected async Task<bool> Take(McsBotPlayerData mcsBotPlayerData, LootData fromLootData, LootData toLootData = null)
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
        protected async Task Sort(InventoryController mcsBotPlayerInventoryController, List<Item> allContainers = null)
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

        protected async Task InteractionDelay(LootData lootData)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(lootData.Item.ExamineTime * 300f));
        }

        protected async Task InteractionDelay(float time)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(time * 1000f));
        }
    }
}