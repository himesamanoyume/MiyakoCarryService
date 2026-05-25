
using System;
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
                    // mcsBotPlayerData.StartLooting();
                    TasksExtensions.HandleExceptions(StartLooting());
                    _lastTimeDistance = Mathf.Infinity; // 重置卡脚检测
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
                if (mcsBotPlayerData.LootingTarget != null)
                {
                    mcsBotPlayerData.IsTaskRunning = true;
                    // yield return new WaitForSeconds(1f);
                    await Task.Delay(1000);

                    var player = BotOwner.GetPlayer;
                    var inventoryController = player.InventoryController;
                    var item = mcsBotPlayerData.LootingTarget?.Item;

                    // 检查物品是否还存在  
                    if (item == null || item.Parent == null)
                    {
                        // yield break;
                        return;
                    }

                    // 获取容器  
                    var rootItem = item.GetRootItem();
                    if (rootItem == null || rootItem.Owner == null)
                    {
                        // MiyakoCarryServicePlugin.Logger.LogWarning("Cannot find container for item");
                        // yield break;
                        return;
                    }

                    var gameWorld = Singleton<GameWorld>.Instance;
                    if (!gameWorld.ItemOwners.TryGetValue(rootItem.Owner, out var itemOwner))
                    {
                        // MiyakoCarryServicePlugin.Logger.LogWarning("Cannot find item owner");
                        // yield break;
                        return;
                    }

                    var lootableContainer = itemOwner.Transform.GetComponent<LootableContainer>();
                    var isActuallyInContainer = lootableContainer != null;

                    // 检查物品是否在容器中  
                    if (isActuallyInContainer)
                    {
                        // 检查容器是否已经打开（使用枚举比较）  
                        if (lootableContainer.DoorState == EDoorState.Shut || lootableContainer.DoorState == EDoorState.Locked)
                        {
                            // 使用BotLootOpener的方式打开容器  
                            var interactionResult = new InteractionResult(EInteractionType.Open);
                            player.CurrentManagedState.StartDoorInteraction(lootableContainer, interactionResult, null);

                            // 等待容器打开动画  
                            // yield return new WaitForSeconds(2.5f);
                            await Task.Delay(2500);

                            // 再次检查容器是否打开成功  
                            if (lootableContainer.DoorState < EDoorState.Open)
                            {
                                BotOwner.TalkMsg(new McsMsg
                                {
                                    PhraseTrigger = EPhraseTrigger.PhraseNone,
                                    Key = Locales.ONLOOTOPENCONTAINERFAILED
                                });
                                // MiyakoCarryServicePlugin.Logger.LogWarning("Failed to open container");
                                // yield break;
                                return;
                            }
                        }

                        // 容器已打开，等待一小段时间让物品刷新  
                        // yield return new WaitForSeconds(3f);
                        await Task.Delay(3000);
                    }

                    // 如果是任务物品，则只是说话提醒，而不拾取
                    if (item.QuestItem)
                    {
                        BotOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.PhraseNone,
                            Key = Locales.ONLOOTQUESTITEM
                        });
                        // MiyakoCarryServicePlugin.Logger.LogWarning($"任务道具不拾取: {item.ShortName}");
                        // yield break;
                        return;
                    }

                    // 优先尝试拾取到背包中
                    var pickupSuccess = await TryPickupToBackpack(mcsBotPlayerData, inventoryController, item, player);

                    if (!pickupSuccess)
                    {
                        // 如果背包拾取失败，尝试拾取到口袋中  
                        pickupSuccess = await TryPickupToPockets(mcsBotPlayerData, inventoryController, item, player);
                    }

                    if (!pickupSuccess)
                    {
                        // 如果口袋拾取失败，尝试拾取到胸挂中  
                        pickupSuccess = await TryPickupToTacticalVest(mcsBotPlayerData, inventoryController, item, player);
                    }

                    if (pickupSuccess)
                    {
                        BotOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.LootGeneric,
                            Key = item.Name
                        });
                    }
                    else
                    {
                        // 护航说话并提示空间不足
                        BotOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.PhraseNone,
                            Key = Locales.ONLOOTNOSPACE
                        });
                        // MiyakoCarryServicePlugin.Logger.LogWarning($"No space for item: {item.ShortName}");
                    }
                }
            }
            finally
            {
                mcsBotPlayerData.IsLooting = false;
                mcsBotPlayerData.IsTaskRunning = false;
            }
            // yield return null;
        }

        private async Task<bool> TryPickupToPockets(McsBotPlayerData mcsBotPlayerData, InventoryController inventoryController, Item item, Player player)
        {
            var pocketsSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Pockets);
            var pockets = pocketsSlot.ContainedItem as SearchableItemItemClass;

            if (pockets == null)
            {
                return false;
            }

            var stashGrid = pockets.Grids.FirstOrDefault();
            if (stashGrid == null)
            {
                return false;
            }

            var location = stashGrid.FindLocationForItem(item);
            if (location != null)
            {
                var moveResult = InteractionsHandlerClass.Move(item, location, inventoryController, true);
                if (moveResult.Succeeded)
                {
                    var rootItem = mcsBotPlayerData.LootingTarget.Item.Owner?.RootItem;
                    var lastOwner = GetLootItemLastOwner(mcsBotPlayerData.LootingTarget);
                    await ExecutePickup(player, moveResult.Value, rootItem, lastOwner);
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> TryPickupToBackpack(McsBotPlayerData mcsBotPlayerData, InventoryController inventoryController, Item item, Player player)
        {
            var backpackSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
            var backpack = backpackSlot.ContainedItem as SearchableItemItemClass;

            if (backpack == null)
            {
                return false;
            }

            var stashGrid = backpack.Grids.FirstOrDefault();
            if (stashGrid == null)
            {
                return false;
            }

            var location = stashGrid.FindLocationForItem(item);
            if (location != null)
            {
                var moveResult = InteractionsHandlerClass.Move(item, location, inventoryController, true);
                if (moveResult.Succeeded)
                {
                    var rootItem = mcsBotPlayerData.LootingTarget.Item.Owner?.RootItem;
                    var lastOwner = GetLootItemLastOwner(mcsBotPlayerData.LootingTarget);
                    await ExecutePickup(player, moveResult.Value, rootItem, lastOwner);
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> TryPickupToTacticalVest(McsBotPlayerData mcsBotPlayerData, InventoryController inventoryController, Item item, Player player)
        {
            var tacticalVestSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
            var tacticalVest = tacticalVestSlot.ContainedItem as SearchableItemItemClass;

            if (tacticalVest == null)
            {
                return false;
            }

            var stashGrid = tacticalVest.Grids.FirstOrDefault();
            if (stashGrid == null)
            {
                return false;
            }

            var location = stashGrid.FindLocationForItem(item);
            if (location != null)
            {
                var moveResult = InteractionsHandlerClass.Move(item, location, inventoryController, true);
                if (moveResult.Succeeded)
                {
                    var rootItem = mcsBotPlayerData.LootingTarget.Item.Owner?.RootItem;
                    var lastOwner = GetLootItemLastOwner(mcsBotPlayerData.LootingTarget);
                    await ExecutePickup(player, moveResult.Value, rootItem, lastOwner);
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> TryPickupToSecureContainer(McsBotPlayerData mcsBotPlayerData, InventoryController inventoryController, Item item, Player player)
        {
            var secureSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer);
            var secureContainer = secureSlot.ContainedItem as SearchableItemItemClass;

            if (secureContainer == null)
            {
                return false;
            }

            var stashGrid = secureContainer.Grids.FirstOrDefault();
            if (stashGrid == null)
            {
                return false;
            }

            var location = stashGrid.FindLocationForItem(item);
            if (location != null)
            {
                var moveResult = InteractionsHandlerClass.Move(item, location, inventoryController, true);
                if (moveResult.Succeeded)
                {
                    var rootItem = mcsBotPlayerData.LootingTarget.Item.Owner?.RootItem;
                    var lastOwner = GetLootItemLastOwner(mcsBotPlayerData.LootingTarget);
                    await ExecutePickup(player, moveResult.Value, rootItem, lastOwner);
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> TryPickupNormally(McsBotPlayerData mcsBotPlayerData, InventoryController inventoryController, Item item, Player player)
        {
            // 检查是否为装备类物品的根物品（避免拾取整个装备）  
            var rootItem = mcsBotPlayerData.LootingTarget.Item.Owner?.RootItem;
            if (rootItem is InventoryEquipment && rootItem == item)
            {
                // 如果物品本身就是装备，仍然尝试拾取到背包中  
                return await TryPickupToBackpack(mcsBotPlayerData, inventoryController, item, player) || await TryPickupToSecureContainer(mcsBotPlayerData, inventoryController, item, player);
            }

            var pickupResult = InteractionsHandlerClass.QuickFindAppropriatePlace(
                item,
                inventoryController,
                inventoryController.Inventory.Equipment.ToEnumerable(),
                InteractionsHandlerClass.EMoveItemOrder.PickUp,
                true
            );

            if (pickupResult.Succeeded && inventoryController.CanExecute(pickupResult.Value))
            {
                var lastOwner = GetLootItemLastOwner(mcsBotPlayerData.LootingTarget);
                await ExecutePickup(player, pickupResult.Value, rootItem, lastOwner);
                return true;
            }

            return false;
        }

        private IPlayer GetLootItemLastOwner(LootData lootData)
        {
            // 尝试从LootItem获取LastOwner  
            if (lootData.RootTransform != null)
            {
                var lootItem = lootData.RootTransform.GetComponent<LootItem>();
                if (lootItem != null)
                {
                    return lootItem.LastOwner;
                }
            }
            return null;
        }

        private async Task ExecutePickup(Player player, GInterface424 pickupAction, Item rootItem, IPlayer lastOwner)
        {
            // 参考BotItemTaker.method_1的实现  
            var pickupCallback = new Callback((IResult result) =>
            {
                if (result.Succeed)
                {
                    player.UpdateInteractionCast();

                    // 触发OnItemTaken事件（如果需要）  
                    if (BotOwner.ItemTaker != null)
                    {
                        // 这里可以触发自定义的拾取完成事件  
                    }
                }
                player.CurrentManagedState.Pickup(false, null);
            });

            // 设置拾取状态  
            player.CurrentManagedState.Pickup(true, new Action(() =>
            {
                // 执行实际的物品移动操作  
                if (pickupAction is GClass3411 gClass3411 && lastOwner != null && lastOwner.ProfileId != player.ProfileId)
                {
                    // 如果是从其他玩家那里拾取的，需要检查弹匣  
                    var magazineItemClass = rootItem as MagazineItemClass;
                    if (magazineItemClass != null)
                    {
                        player.InventoryController.StrictCheckMagazine(magazineItemClass, false, 0, false, true);
                    }

                    var containerClass = rootItem as ContainerClass;
                    if (containerClass != null)
                    {
                        foreach (var mag in containerClass.GetAllItemsFromCollection().OfType<MagazineItemClass>())
                        {
                            player.InventoryController.StrictCheckMagazine(mag, false, 0, false, true);
                        }
                    }

                    // 刷新医疗物品  
                    BotOwner.AITaskManager.RegisterDelayedTask(BotOwner, 0.5f, new Action(BotOwner.Medecine.RefreshCurMeds));
                }

                // 执行网络事务  
                player.InventoryController.RunNetworkTransaction(pickupAction, pickupCallback);
            }));

            await OrganizeContainersAfterPickup(player.InventoryController, player);
        }

        private async Task OrganizeContainersAfterPickup(InventoryController inventoryController, Player player)
        {
            var backpackSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
            if (backpackSlot.ContainedItem != null && backpackSlot.ContainedItem is SearchableItemItemClass backpack)
            {
                var sortResult = InteractionsHandlerClass.Sort(backpack, inventoryController, true);
                if (sortResult.Succeeded)
                {
                    await inventoryController.TryRunNetworkTransaction(sortResult, null);
                }
            }

            var tacticalVestSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
            if (tacticalVestSlot.ContainedItem != null && tacticalVestSlot.ContainedItem is SearchableItemItemClass tacticalVest)
            {
                var sortResult = InteractionsHandlerClass.Sort(tacticalVest, inventoryController, true);
                if (sortResult.Succeeded)
                {
                    await inventoryController.TryRunNetworkTransaction(sortResult, null);
                }
            }
        }
    }
}