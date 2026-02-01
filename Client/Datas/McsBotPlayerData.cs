

using System;
using System.Collections;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Bots.BotBehaviors;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Misc;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    internal sealed class McsBotPlayerData : PlayerData
    {
        private WeakReference<BotOwner> _botOwnerRef;
        public BotOwner BotOwner => _botOwnerRef.TryGetTarget(out var botOwner) ? botOwner : null;
        private WeakReference<Player> _bossPlayeRef;
        public Player BossPlayer => _bossPlayeRef.TryGetTarget(out var bossPlayer) ? bossPlayer : null;
        public List<BotBehavior> BotBehaviors { get; private set; }
        private WeakReference<McsAIBossPlayer> _mcsAIBossPlayerRef;
        public McsAIBossPlayer McsAIBossPlayer => _mcsAIBossPlayerRef.TryGetTarget(out var mcsAIBossPlayer) ? mcsAIBossPlayer : null;
        private List<LootData> _filtedLootDatas;
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
                    _lootingTarget  = null;
                }
            }
        }
        private LootDataMgr _lootDataMgr = null;
        public bool IsRunningCoroutine = false;
        public McsBotPlayerData(Player bossPlayer, McsAIBossPlayer mcsAIBossPlayer, Player player, Item item) : base(player, item)
        {
            _botOwnerRef = new(player.AIData.BotOwner);
            _mcsAIBossPlayerRef = new(mcsAIBossPlayer);
            _bossPlayeRef = new(bossPlayer);
            BotBehaviors = [new BotCarryServiceChecker(BotOwner, BossPlayer)];
            _lootDataMgr = _gameloop.GetMgr<LootDataMgr>();
        }

        public void SetLootingTarget(List<ItemData> itemDatas)
        {
            if (!IsLooting)
            {
                LootingTarget = null;
                return;
            }

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

                if (!lootData.LootProps.TryGetValue(McsAIBossPlayer, out var lootProp))
                {
                    continue;
                }

                if (lootProp.IsBlockItem)
                {
                    continue;
                }

                if (!lootProp.IsWishListItem && !lootProp.IsHighPriceItem && !lootProp.IsQuestNeedItem)
                {
                    continue;
                }

                filtedLootDatas.Add(lootData);
            }

            filtedLootDatas.Sort((a, b) => b.Offer.Price.CompareTo(a.Offer.Price));
            _filtedLootDatas = filtedLootDatas;
            foreach (var lootData in _filtedLootDatas)
            {
                if (!_lootDataMgr.IsLootingTarget(lootData))
                {
                    continue;
                }

                // 此处不够完善，比如如果一个战利品是任务物品，但价值很低，会导致其优先级很低，后续仍需完善
                // 以及还有剩余的武器、护甲等类型物品的筛选未实现
                if (!Player.HandsController.SupportPickup())
                {
                    continue;
                }

                // 这里只判断了当前是否可拿，但如果是特殊情况的话，比如没空位了需要拿医疗物品，就还需要进行物品交换
                var pickUpResult = InteractionsHandlerClass.QuickFindAppropriatePlace(lootData.Item, Player.InventoryController, Player.InventoryController.Inventory.Equipment.ToEnumerable(), InteractionsHandlerClass.EMoveItemOrder.PickUp, true);

                if (!pickUpResult.Succeeded || !Player.InventoryController.CanExecute(pickUpResult.Value))
                {
                    continue;
                }

                _lootDataMgr.LockLootItemToTarget(lootData);
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
            try
            {
                if (LootingTarget != null)
                {
                    var internalSearchTime = new WaitForSeconds(0.5f);
                    IsRunningCoroutine = true;
                    // 模拟打开容器的时间
                    yield return new WaitForSeconds(2f);
                    var rootItem = LootingTarget.Item.GetRootItem();
                    if (rootItem.IsContainer && rootItem is SearchableItemItemClass searchableItemItemClass)
                    {
                        var lockedItems = new List<Item>();
                        if (searchableItemItemClass is CompoundItem compoundItem && compoundItem.Slots != null)
                        {
                            foreach (var slot in compoundItem.Slots)
                            {
                                if (slot.Locked && slot.Items != null)
                                {
                                    lockedItems.AddRange(slot.Items);
                                }
                            }
                        }
                        
                        foreach (var nestedItem in searchableItemItemClass.GetFirstLevelItems())
                        {
                            if (BotOwner.Memory.HaveEnemy)
                            {
                                break;
                            }

                            // 模拟搜索单格物品等待时间
                            yield return internalSearchTime;
                            if (!lockedItems.Contains(nestedItem))
                            {
                                // Player.InventoryController.TakeLoot(nestedItem, nestedItem.IncludeTargetItem(LootingTarget.Item));
                            }
                        }
                    }
                    else
                    {
                        
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
    }
}