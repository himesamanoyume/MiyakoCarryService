
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
        private float _lastTimeDistance = -1f;
        public GoToLootTargetLogic(BotOwner botOwner) : base(botOwner)
        {

        }

        public override void Start()
        {
            base.Start();
            BotOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.GoLoot
            });
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

        public override void Update(CustomLayer.ActionData data)
        {
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData.IsTaskRunning)
            {
                return;
            }

            mcsBotPlayerData.IsLooting = true;

            if (_lastTimeCheckDistance < Time.time)
            {
                _currentLootingRetries++;
                if (_currentLootingRetries > 30)
                {
                    // MiyakoCarryServicePlugin.Logger.LogWarning("重试超时");
                    mcsBotPlayerData.IsLooting = false;
                    _currentLootingRetries = 0;
                    return;
                }

                _lastTimeCheckDistance = Time.time + 2f;

                var lootPos = mcsBotPlayerData.LootingTarget.RootTransform.position;
                var offset = BotOwner.Position - lootPos;
                var distance = BotOwner.Position.McsSqrDistance(lootPos);

                Tools.BetterDestination(3f, lootPos, out var targetPos);

#if DEBUG
                MiyakoCarryServicePlugin.Logger.LogWarning($"{mcsBotPlayerData.Player.Profile.Nickname}, 目标: {mcsBotPlayerData.LootingTarget.Item.Name.McsLocalized()}, 价值: {mcsBotPlayerData.LootingTarget.Offer.Price}, 坐标: {targetPos}, Sqr距离: {distance}, 高度差: {Math.Abs(offset.y)}");
#endif

                // 到达判定
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
                    _lastTimeDistance = Mathf.Infinity;
                    return;
                }

                // 移动控制
                if (distance <= 5f)
                {
                    BotOwner.SetTargetMoveSpeed(1f);
                    BotOwner.Steering.LookToMovingDirection();
                    BotOwner.Mover.Sprint(false);
                }

                var pathStatus = BotOwner.GoToPoint(targetPos, mustHaveWay: true);
                if (pathStatus != NavMeshPathStatus.PathComplete)
                {
                    // MiyakoCarryServicePlugin.Logger.LogWarning("没有路径");
                    mcsBotPlayerData.IsLooting = false;
                    return;
                }

                if (_lastTimeDistance > 0f)
                {
                    BotOwner.CheckStuck();
                }

                _lastTimeDistance = distance;
            }
        }

        private async Task StartLooting()
        {
            // MiyakoCarryServicePlugin.Logger.LogWarning("开始掠夺目标战利品");
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            try
            {
                if (mcsBotPlayerData.LootingTarget == null)
                {
                    return;
                }

                mcsBotPlayerData.IsTaskRunning = true;
                await Task.Delay(1000);

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
                mcsBotPlayerData.IsLooting = false;
                mcsBotPlayerData.IsTaskRunning = false;
            }
        }

        private async Task Execute(McsBotPlayerData mcsBotPlayerData, GInterface424 action, LootData targetLootData, bool isPickUp = true)
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

                if (isPickUp)
                {
                    mcsBotPlayer.CurrentManagedState.Pickup(false, null);
                }
                else
                {
                    mcsBotPlayer.CurrentManagedState.OnInventory(false);
                }
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
                    targetLootData.ItemType == EItemType.Rig ? mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

            if (currentSlot == null)
            {
                normalTake = true;
            }

            if (normalTake)
            {
                await InteractionDelay(targetLootData);
                return await Take(mcsBotPlayerData, targetLootData);
            }
            else
            {
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

                if (lootProp.IsShouldEquipContainer(mcsBotPlayerData.BotOwner))
                {
                    return await Equip(mcsBotPlayerData, targetLootData);
                }
                else if (lootProp.IsShouldSwapContainer(mcsBotPlayerData.BotOwner))
                {
                    return await Swap(mcsBotPlayerData, targetLootData);
                }
                else if (lootProp.IsShouldTakeContainer(mcsBotPlayerData.BotOwner))
                {
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
                    targetLootData.ItemType == EItemType.Rig ? mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest) : null;

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

            if (targetLootData.ItemType == EItemType.Rig || (targetLootData.ItemType == EItemType.Backpack && !currentLootData.IsContainerWithAdditionalGrid))
            {
                await Throw(mcsBotPlayerData, currentLootData, targetLootData);
            }
            await InteractionDelay(1);
            await Equip(mcsBotPlayerData, targetLootData);
            return true;
        }

        private async Task<bool> Equip(McsBotPlayerData mcsBotPlayerData, LootData targetLootData)
        {
            var mcsBotPlayerInventoryController = mcsBotPlayerData.Player.InventoryController;

            var targets = new List<CompoundItem>();

            var rigSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
            if (rigSlot.ContainedItem != null && rigSlot.ContainedItem is CompoundItem rig)
            {
                targets.Add(rig);
            }

            var backpckSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
            if (backpckSlot.ContainedItem != null && backpckSlot.ContainedItem is CompoundItem backpack)
            {
                targets.Add(backpack);
            }

            var result = InteractionsHandlerClass.QuickFindAppropriatePlace(
                targetLootData.Item,
                mcsBotPlayerInventoryController,
                targets,
                InteractionsHandlerClass.EMoveItemOrder.TryEquip,
                true
            );

            if (result.Succeeded)
            {
                await Execute(mcsBotPlayerData, result.Value, targetLootData);
                await Sort(mcsBotPlayerInventoryController);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 丢东西之前需要先将内部的物品进行转移
        /// </summary>
        /// <returns></returns>
        private async Task Throw(McsBotPlayerData mcsBotPlayerData, LootData throwLootData, LootData retainLootData)
        {
            await Transfer(mcsBotPlayerData, throwLootData, retainLootData);
            var promise = new TaskCompletionSource<IResult>();
            mcsBotPlayerData.Player.InventoryController.ThrowItem(throwLootData.Item, false, promise.SetResult);
        }

        /// <summary>
        /// 转移原容器中最顶层的物品至新容器
        /// </summary>
        /// <param name="mcsBotPlayerData"></param>
        /// <param name="fromLootData"></param>
        /// <param name="toLootData"></param>
        /// <returns></returns>
        private async Task Transfer(McsBotPlayerData mcsBotPlayerData, LootData fromLootData, LootData toLootData)
        {
            if (fromLootData.Item is ContainerClass containerClass)
            {
                var firstLevelItems = containerClass.GetFirstLevelItems();
                foreach (var firstLevelItem in firstLevelItems)
                {
                    var firstLevelItemData = firstLevelItem.GetData();
                    if (firstLevelItem == null)
                    {
                        continue;
                    }

                    if (firstLevelItemData is LootData firstLevelLootData)
                    {
                        await InteractionDelay(1);
                        await Take(mcsBotPlayerData, firstLevelLootData, toLootData);
                    }
                }
            }
        }

        /// <summary>
        /// 拾取不再只是单纯往胸挂、背包、口袋槽位内拾取，而是包括胸挂、背包、口袋本身以外还有其中嵌套的容器，全部都要进行尝试放入
        /// </summary>
        /// <param name="mcsBotPlayerData"></param>
        /// <param name="fromLootData"></param>
        /// <returns></returns>
        private async Task<bool> Take(McsBotPlayerData mcsBotPlayerData, LootData fromLootData, LootData toLootData = null)
        {
            var mcsBotPlayerInventoryController = mcsBotPlayerData.Player.InventoryController;
            var targets = new List<CompoundItem>();

            if (toLootData != null && toLootData.Item is CompoundItem toContainer)
            {
                targets.Add(toContainer);
            }
            else
            {
                var pocketsSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Pockets);
                if (pocketsSlot.ContainedItem != null && pocketsSlot.ContainedItem is CompoundItem pocket)
                {
                    targets.Add(pocket);
                }

                var rigSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
                if (rigSlot.ContainedItem != null && rigSlot.ContainedItem is CompoundItem rig)
                {
                    targets.Add(rig);
                }

                var backpckSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
                if (backpckSlot.ContainedItem != null && backpckSlot.ContainedItem is CompoundItem backpack)
                {
                    targets.Add(backpack);
                }
            }

            var result = InteractionsHandlerClass.QuickFindAppropriatePlace(
                fromLootData.Item,
                mcsBotPlayerInventoryController,
                targets,
                InteractionsHandlerClass.EMoveItemOrder.PickUp | InteractionsHandlerClass.EMoveItemOrder.TryTransfer,
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
        /// <param name="mcsBotPlayerInventoryController"></param>
        /// <param name="allContainers"></param>
        /// <returns></returns>
        private async Task Sort(InventoryController mcsBotPlayerInventoryController, List<Item> allContainers = null)
        {
            await InteractionDelay(2);

            if (allContainers == null)
            {
                allContainers = new();

                var rigSlot = mcsBotPlayerInventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
                if (rigSlot.ContainedItem != null)
                {
                    rigSlot.ContainedItem.GetAllItemsNonAlloc(allContainers, false, false);
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
            await Task.Delay(TimeSpan.FromMilliseconds(time * 300f));
        }
    }
}