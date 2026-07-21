

using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class McsBotPlayerData : PlayerData
    {
        private WeakReference<BotOwner> _botOwnerRef;
        public BotOwner BotOwner => _botOwnerRef.TryGetTarget(out var botOwner) ? botOwner : null;
        private WeakReference<Player> _leadPlayeRef;
        public Player LeadPlayer => _leadPlayeRef.TryGetTarget(out var leadPlayer) ? leadPlayer : null;
        public GamePlayerOwner LeadPlayerGamePlayerOwner => McsAILeadPlayer.GamePlayerOwner;
        private WeakReference<McsAILeadPlayer> _mcsAILeadPlayerRef;
        public McsAILeadPlayer McsAILeadPlayer => _mcsAILeadPlayerRef.TryGetTarget(out var mcsAILeadPlayer) ? mcsAILeadPlayer : null;
        public BodyPartType AimingBodyPartType = BodyPartType.head;
        public Vector3? TargetPos = null;
        public string ProxyTargetId = null;
        public LootData LootingTarget = null;
        public List<Vector3> ClearAreaPoints = null;  
        public int ClearAreaIndex = 0;  
        public float ClearAreaLookAroundUntil = 0f;
        public bool IsLooting
        {
            get => field;
            set
            {
                field = value;
                if (!field && LootingTarget != null)
                {
                    LootDataMgr.UnlockLootingTarget(LootingTarget);
                    LootDataMgr.UnlockLootingTargetRootTransform(LootingTarget.RootTransform);
                    LootingTarget = null;
                }
            }
        }
        public bool IsTaskRunning = false;
        private readonly HashSet<string> _decisions = new();
        private HashSet<LootData> _vanishingCurseLootItems = new();
        public bool IsMcsLayerActive = false;

        public void SetDecision(string[] exclude = null, params string[] decisions)
        {
            List<string> preserved = null;
            if (exclude != null)
            {
                preserved = new List<string>();
                foreach (var e in exclude)
                {
                    if (_decisions.Contains(e))
                    {
                        preserved.Add(e);
                    }
                }
            }

            _decisions.Clear();

            if (decisions != null)
            {
                foreach (var decision in decisions)
                {
                    _decisions.Add(decision);
                }
            }

            if (preserved != null)
            {
                foreach (var p in preserved)
                {
                    _decisions.Add(p);
                }
            }
        }

        public bool HasDecision(params string[] decisions)
        {
            foreach (var decision in decisions)
            {
                if (!_decisions.Contains(decision))
                {
                    return false;
                }
            }
            return true;
        }

        public void AddDecision(params string[] decisions)
        {
            foreach (var decision in decisions)
            {
                _decisions.Add(decision);
            }
        }

        public void RemoveDecision(params string[] decisions)
        {
            foreach (var decision in decisions)
            {
                _decisions.Remove(decision);
            }
        }

        public McsBotPlayerData(Player bossPlayer, McsAILeadPlayer mcsAILeadPlayer, Player player, Item item) : base(player, item)
        {
            _botOwnerRef = new(player.AIData.BotOwner);
            BotOwner.SetMcsBotPlayerData(this);
            _mcsAILeadPlayerRef = new(mcsAILeadPlayer);
            _leadPlayeRef = new(bossPlayer);
            CollectVanishingCurseLootItems();
        }

        public void CollectVanishingCurseLootItems()
        {
            if (_vanishingCurseLootItems == null)
            {
                _vanishingCurseLootItems = new();
            }

            var slots = InventoryEquipment.AllSlotNames
                .Where(slotName => slotName is not EquipmentSlot.Dogtag)
                .Select(BotOwner.Profile.Inventory.Equipment.GetSlot).ToArray();

            foreach (var slot in slots)
            {
                if (slot.ContainedItem == null)
                {
                    continue;
                }

                var allItems = slot.ContainedItem.GetAllItems();
                foreach (var item in allItems)
                {
                    var itemData = item.GetData();
                    if (itemData == null)
                    {
                        continue;
                    }

                    if (itemData is not LootData lootData)
                    {
                        continue;
                    }

                    if (lootData.ItemType is EItemType.Backpack or EItemType.Equipment)
                    {
                        continue;
                    }

                    if (lootData.VanishingCurse)
                    {
                        _vanishingCurseLootItems.Add(lootData);
                    }
                }
            }
        }

        public void SetLootingTarget(List<ItemData> itemDatas)
        {
            if (HasDecision(Decisions.ShouldLootProxyAction))
            {
                return;
            }

            var filtedLootDatas = new List<LootData>(itemDatas.Count);
            var usefulContainers = new List<LootData>();
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

                if (lootData.IsInSecureContainerItem)
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

                if (lootProp.IsLootOnColdown(BotOwner))
                {
                    continue;
                }

                if (lootProp.IsShouldTakeContainer(BotOwner) || lootProp.IsShouldEquipContainer(BotOwner) || lootProp.IsShouldSwapContainer(BotOwner) || lootProp.IsShouldNestContainer(BotOwner) is ENestType.In or ENestType.Out)
                {
                    usefulContainers.Add(lootData);
                    continue;
                }

                if (!lootProp.IsHighPriceItem && (!McsAILeadPlayer.McsBotPlayerConfig.LootingKeywordItem || !lootProp.IsKeywordItem))
                {
                    continue;
                }

                filtedLootDatas.Add(lootData);
            }

            usefulContainers.Sort((a, b) => b.ContainerGridCount.CompareTo(a.ContainerGridCount));
            foreach (var containerData in usefulContainers)
            {
                if (LootDataMgr.IsLockedLootingTarget(containerData))
                {
                    continue;
                }

                if (LootDataMgr.IsLockedLootingTargetRootTransform(containerData.RootTransform))
                {
                    continue;
                }

                LootDataMgr.LockLootItemToTarget(containerData);
                LootDataMgr.LockLootingTargetRootTransform(containerData.RootTransform);
                LootingTarget = containerData;
                return;
            }

            filtedLootDatas.Sort((a, b) => b.Offer.Price.CompareTo(a.Offer.Price));
            foreach (var lootData in filtedLootDatas)
            {
                if (LootDataMgr.IsLockedLootingTarget(lootData))
                {
                    continue;
                }

                if (LootDataMgr.IsLockedLootingTargetRootTransform(lootData.RootTransform))
                {
                    continue;
                }

                LootDataMgr.LockLootItemToTarget(lootData);
                LootDataMgr.LockLootingTargetRootTransform(lootData.RootTransform);
                LootingTarget = lootData;
                return;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _botOwnerRef = null;
            _leadPlayeRef = null;
            _mcsAILeadPlayerRef = null;
            IsLooting = false;
            LootingTarget = null;
        }

        public void HandleBalanceRestriction()
        {
            foreach (var lootData in _vanishingCurseLootItems)
            {
                var item = lootData.Item;
                if (item?.CurrentAddress == null || !lootData.VanishingCurse)
                {
                    continue;
                }

                if (item.CurrentAddress.Container is Slot slot && slot.ContainedItem == item && Enum.TryParse<EquipmentSlot>(slot.ID, out var equipmentSlot))
                {
                    if (equipmentSlot is EquipmentSlot.Backpack or EquipmentSlot.TacticalVest or EquipmentSlot.Pockets)
                    {
                        continue;
                    }

                    var parentItem = item.Parent.GetRootItem();
                    var itemData = parentItem.GetData();
                    if (itemData is PlayerData playerData)
                    {
                        if ((equipmentSlot is EquipmentSlot.FirstPrimaryWeapon && playerData.Player.HandsController.Item == item)
                        || (equipmentSlot is EquipmentSlot.SecondPrimaryWeapon && playerData.Player.HandsController.Item == item)
                        || (equipmentSlot is EquipmentSlot.Holster && playerData.Player.HandsController.Item == item)
                        || (equipmentSlot is EquipmentSlot.Scabbard && playerData.Player.HandsController.Item == item))
                        {
                            continue;
                        }
                    }

                    slot.RemoveItemWithoutRestrictions();
                }
                else
                {
                    item.McsRemoveItem();
                }
            }
        }
    }
}