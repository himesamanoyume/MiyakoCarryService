

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Bots.BotBehaviors;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public sealed class McsBotPlayerData : PlayerData
    {
        private WeakReference<BotOwner> _botOwnerRef;
        public BotOwner BotOwner => _botOwnerRef.TryGetTarget(out var botOwner) ? botOwner : null;
        private WeakReference<Player> _leadPlayeRef;
        public Player LeadPlayer => _leadPlayeRef.TryGetTarget(out var leadPlayer) ? leadPlayer : null;
        public List<BotBehavior> BotBehaviors { get; private set; }
        public GamePlayerOwner McsLeadPlayerGamePlayerOwner => McsAILeadPlayer.GamePlayerOwner;
        private WeakReference<McsAILeadPlayer> _mcsAILeadPlayerRef;
        public McsAILeadPlayer McsAILeadPlayer => _mcsAILeadPlayerRef.TryGetTarget(out var mcsAILeadPlayer) ? mcsAILeadPlayer : null;
        private LootData _lootingTarget = null;
        private bool _isLooting = false;
        public LootData LootingTarget
        {
            get => _lootingTarget;
            set
            {
                if (_lootingTarget != null)
                {
                    _lootDataMgr.UnlockLootingTarget(_lootingTarget);
                    _lootDataMgr.UnlockLootingTargetRootTransform(_lootingTarget.RootTransform);
                }
                _lootingTarget = value;
            }
        }
        public bool IsLooting
        {
            get => _isLooting;
            set
            {
                if (_isLooting == value)
                {
                    return;
                }

                _isLooting = value;
                if (!_isLooting && _lootingTarget != null)
                {
                    _lootDataMgr.UnlockLootingTarget(_lootingTarget);
                    _lootDataMgr.UnlockLootingTargetRootTransform(_lootingTarget.RootTransform);
                    _lootingTarget = null;
                }
            }
        }
        public bool IsRunningCoroutine = false;

        public bool ShouldHoldPosition = false;
        public bool ShouldGoToPoint = false;
        public EStrategy Strategy = EStrategy.Default;

        public McsBotPlayerData(Player bossPlayer, McsAILeadPlayer mcsAILeadPlayer, Player player, Item item) : base(player, item)
        {
            _botOwnerRef = new(player.AIData.BotOwner);
            _mcsAILeadPlayerRef = new(mcsAILeadPlayer);
            _leadPlayeRef = new(bossPlayer);
            BotBehaviors = [new BotCarryServiceChecker(BotOwner, LeadPlayer)];
        }

        public void SetLootingTarget(List<ItemData> itemDatas)
        {
            // MiyakoCarryServicePlugin.Logger.LogWarning("正在设置目标战利品");
            var filtedLootDatas = new List<LootData>(itemDatas.Count);
            // 只要是符合条件的，都先筛选出来
            foreach (var itemData in itemDatas)
            {
                if (itemData == null)
                {
                    continue;
                }

                if (itemData is not LootData lootData)
                {
                    continue;
                }

                if (lootData.IsNonNavigableItem)
                {
                    continue;
                }

                if (!lootData.LootProps.TryGetValue(McsAILeadPlayer, out var lootProp))
                {
                    continue;
                }

                if (lootProp.IsBlockItem)
                {
                    continue;
                }

                if (!lootProp.IsHighPriceItem && !lootProp.IsKeywordItem)
                {
                    continue;
                }

                filtedLootDatas.Add(lootData);
            }

            filtedLootDatas.Sort((a, b) => b.Offer.Price.CompareTo(a.Offer.Price));
            foreach (var lootData in filtedLootDatas)
            {
                if (_lootDataMgr.IsLockedLootingTarget(lootData))
                {
                    continue;
                }

                if (_lootDataMgr.IsLockedLootingTargetRootTransform(lootData.RootTransform))
                {
                    continue;
                }

                _lootDataMgr.LockLootItemToTarget(lootData);
                _lootDataMgr.LockLootingTargetRootTransform(lootData.RootTransform);
                LootingTarget = lootData;
                return;
            }
            LootingTarget = null;
        }

        public void UnlockLootingTarget()
        {
            IsLooting = false;
        }

        public void StartLooting()
        {
            _gameloop.StartCoroutine(InternalStartLooting());
        }

        private IEnumerator InternalStartLooting()
        {
            MiyakoCarryServicePlugin.Logger.LogWarning("开始掠夺目标战利品");
            try
            {
                if (LootingTarget != null)
                {
                    IsRunningCoroutine = true;
                    yield return new WaitForSeconds(1f);

                    var player = BotOwner.GetPlayer;
                    var inventoryController = player.InventoryController;
                    var item = LootingTarget.Item;

                    // 检查物品是否还存在  
                    if (item == null || item.Parent == null)
                    {
                        MiyakoCarryServicePlugin.Logger.LogWarning($"Item {LootingTarget.Item?.ShortName} no longer exists");
                        yield break;
                    }

                    // 获取容器  
                    var rootItem = item.GetRootItem();
                    if (rootItem == null || rootItem.Owner == null)
                    {
                        MiyakoCarryServicePlugin.Logger.LogWarning("Cannot find container for item");
                        yield break;
                    }

                    var gameWorld = Singleton<GameWorld>.Instance;
                    if (!gameWorld.ItemOwners.TryGetValue(rootItem.Owner, out var itemOwner))
                    {
                        MiyakoCarryServicePlugin.Logger.LogWarning("Cannot find item owner");
                        yield break;
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
                            yield return new WaitForSeconds(2.5f);

                            // 再次检查容器是否打开成功  
                            if (lootableContainer.DoorState != EDoorState.Open)
                            {
                                MiyakoCarryServicePlugin.Logger.LogWarning("Failed to open container");
                                yield break;
                            }
                        }

                        // 容器已打开，等待一小段时间让物品刷新  
                        yield return new WaitForSeconds(0.5f);
                    }

                    // 如果是任务物品，则只是说话提醒，而不拾取
                    if (item.QuestItem)
                    {
                        MiyakoCarryServicePlugin.Logger.LogWarning($"任务道具不拾取: {item.ShortName}");
                        yield break;
                    }

                    // 优先尝试拾取到背包中
                    var pickupSuccess = TryPickupToBackpack(item, player);

                    if (!pickupSuccess)
                    {
                        // 如果背包拾取失败，尝试拾取到口袋中  
                        pickupSuccess = TryPickupToPockets(item, player);
                    }

                    if (!pickupSuccess)
                    {
                        // 如果口袋拾取失败，尝试拾取到胸挂中  
                        pickupSuccess = TryPickupToTacticalVest(item, player);
                    }

                    if (pickupSuccess)
                    {
                        var containerText = LootingTarget.IsItemInContainer ? "从容器中" : "";
                        MiyakoCarryServicePlugin.Logger.LogWarning(string.Format("{0} 从 {1} 拾取了 {2}", BotOwner.Profile.Nickname, containerText, item.ShortName.McsLocalized()));
                    }
                    else
                    {
                        // 护航说话并提示空间不足
                        MiyakoCarryServicePlugin.Logger.LogWarning($"No space for item: {item.ShortName}");
                    }
                }
            }
            finally
            {
                UnlockLootingTarget();
                IsRunningCoroutine = false;
            }
            yield return null;
        }

        private bool TryPickupToPockets(Item item, Player player)
        {
            var inventoryController = player.InventoryController;
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
                    var rootItem = LootingTarget.Item.Owner?.RootItem;
                    var lastOwner = GetLootItemLastOwner(LootingTarget);
                    ExecutePickup(player, moveResult.Value, rootItem, lastOwner);
                    return true;
                }
            }

            return false;
        }

        private bool TryPickupToBackpack(Item item, Player player)
        {
            var inventoryController = player.InventoryController;
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
                    var rootItem = LootingTarget.Item.Owner?.RootItem;
                    var lastOwner = GetLootItemLastOwner(LootingTarget);
                    ExecutePickup(player, moveResult.Value, rootItem, lastOwner);
                    return true;
                }
            }

            return false;
        }

        private bool TryPickupToTacticalVest(Item item, Player player)
        {
            var inventoryController = player.InventoryController;
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
                    var rootItem = LootingTarget.Item.Owner?.RootItem;
                    var lastOwner = GetLootItemLastOwner(LootingTarget);
                    ExecutePickup(player, moveResult.Value, rootItem, lastOwner);
                    return true;
                }
            }

            return false;
        }

        private bool TryPickupToSecureContainer(Item item, Player player)
        {
            var inventoryController = player.InventoryController;
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
                    var rootItem = LootingTarget.Item.Owner?.RootItem;
                    var lastOwner = GetLootItemLastOwner(LootingTarget);
                    ExecutePickup(player, moveResult.Value, rootItem, lastOwner);
                    return true;
                }
            }

            return false;
        }

        private bool TryPickupNormally(Item item, Player player)
        {
            var inventoryController = player.InventoryController;

            // 检查是否为装备类物品的根物品（避免拾取整个装备）  
            var rootItem = LootingTarget.Item.Owner?.RootItem;
            if (rootItem is InventoryEquipment && rootItem == item)
            {
                // 如果物品本身就是装备，仍然尝试拾取到背包中  
                return TryPickupToBackpack(item, player) || TryPickupToSecureContainer(item, player);
            }

            var pickupResult = InteractionsHandlerClass.QuickFindAppropriatePlace(
                item,
                inventoryController,
                inventoryController.Inventory.Equipment.ToEnumerable<InventoryEquipment>(),
                InteractionsHandlerClass.EMoveItemOrder.PickUp,
                true
            );

            if (pickupResult.Succeeded && inventoryController.CanExecute(pickupResult.Value))
            {
                var lastOwner = GetLootItemLastOwner(LootingTarget);
                ExecutePickup(player, pickupResult.Value, rootItem, lastOwner);
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

        private void ExecutePickup(Player player, GInterface424 pickupAction, Item rootItem, IPlayer lastOwner)
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
        }
    }
}