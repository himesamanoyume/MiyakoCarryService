

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
        private HashSet<string> _intents = new();
        private HashSet<LootData> _vanishingCurseLootItems = new();
        private const float EMERGENCY_NEED_CHECK_INTERVAL = 5f;
        private const float AMMO_SUFFICIENT_MULTIPLIER = 3f;
        private const int DEFAULT_MAG_CAPACITY = 30;
        private const int REQUIRED_MAG_COUNT = 3;
        private float _nextEmergencyNeedCheckTime = 0f;
        private ELootNeedType _cachedEmergencyLootNeed = ELootNeedType.None;
        private HashSet<EDamageEffectType> _missingMedEffects = new();
        public bool IsMcsLayerActive = false;
        public bool IsBtrLeaving = false;
        public byte BtrTargetSide = 0;
        public byte BtrTargetSlot = 0;
        public bool IsExcluded = false;

        public void SetIntent(string[] exclude = null, params string[] intents)
        {
            List<string> preserved = null;
            if (exclude != null)
            {
                preserved = new List<string>();
                foreach (var e in exclude)
                {
                    if (_intents.Contains(e))
                    {
                        preserved.Add(e);
                    }
                }
            }

            _intents.Clear();

            if (intents != null)
            {
                foreach (var intent in intents)
                {
                    _intents.Add(intent);
                }
            }

            if (preserved != null)
            {
                foreach (var p in preserved)
                {
                    _intents.Add(p);
                }
            }
        }

        public bool HasIntent(params string[] intents)
        {
            foreach (var intent in intents)
            {
                if (!_intents.Contains(intent))
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasAnyIntent(params string[] intents)
        {
            if (intents == null)
            {
                return false;
            }

            foreach (var intent in intents)
            {
                if (_intents.Contains(intent))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddIntent(params string[] intents)
        {
            foreach (var intent in intents)
            {
                _intents.Add(intent);
            }
        }

        public void RemoveIntent(params string[] intents)
        {
            foreach (var intent in intents)
            {
                _intents.Remove(intent);
            }
        }

        public McsBotPlayerData(Player bossPlayer, McsAILeadPlayer mcsAILeadPlayer, Player player, Item item) : base(player, item)
        {
            _botOwnerRef = new(player.AIData.BotOwner);
            BotOwner.SetMcsBotPlayerData(this);
            _mcsAILeadPlayerRef = new(mcsAILeadPlayer);
            _leadPlayeRef = new(bossPlayer);
            CollectVanishingCurseLootItems();
            if (mcsAILeadPlayer.McsBotPlayerConfig.EnableKeepFormation)
            {
                AddIntent(Intents.ShouldKeepFormation);
            }
            else
            {
                RemoveIntent(Intents.ShouldKeepFormation);
            }
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
            if (HasIntent(Intents.ShouldLootProxyAction))
            {
                return;
            }

            var emergencyLootNeed = GetEmergencyLootNeed();
            if (emergencyLootNeed != ELootNeedType.None)
            {
                if (TrySetEmergencyLootingTarget(itemDatas, emergencyLootNeed))
                {
                    return;
                }

                var botOwner = BotOwner;
                if (emergencyLootNeed != ELootNeedType.Ammo && botOwner != null && NeedAmmo(botOwner) && TrySetEmergencyLootingTarget(itemDatas, ELootNeedType.Ammo))
                {
                    return;
                }

                if (emergencyLootNeed != ELootNeedType.Meds && GetMissingMedEffects().Count > 0 && TrySetEmergencyLootingTarget(itemDatas, ELootNeedType.Meds))
                {
                    return;
                }

                return;
            }

            if (!McsAILeadPlayer.McsBotPlayerConfig.EnableLooting)
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

        public ELootNeedType GetEmergencyLootNeed()
        {
            if (Time.time < _nextEmergencyNeedCheckTime)
            {
                return _cachedEmergencyLootNeed;
            }

            _nextEmergencyNeedCheckTime = Time.time + EMERGENCY_NEED_CHECK_INTERVAL;
            _cachedEmergencyLootNeed = ELootNeedType.None;

            var botOwner = BotOwner;
            var player = Player;
            if (botOwner == null || player == null)
            {
                return _cachedEmergencyLootNeed;
            }

            var weaponManager = botOwner.WeaponManager;
            var inventoryController = player.InventoryController;
            if (weaponManager == null || inventoryController == null)
            {
                return _cachedEmergencyLootNeed;
            }

            if (NeedMagazine(weaponManager.CurrentWeapon, inventoryController))
            {
                _cachedEmergencyLootNeed = ELootNeedType.Magazine;
                return _cachedEmergencyLootNeed;
            }

            if (NeedAmmo(botOwner))
            {
                _cachedEmergencyLootNeed = ELootNeedType.Ammo;
                return _cachedEmergencyLootNeed;
            }

            if (GetMissingMedEffects().Count > 0)
            {
                _cachedEmergencyLootNeed = ELootNeedType.Meds;
                return _cachedEmergencyLootNeed;
            }

            return _cachedEmergencyLootNeed;
        }

        public bool HasEmergencyLootNeed()
        {
            return GetEmergencyLootNeed() != ELootNeedType.None;
        }

        private bool NeedMagazine(Weapon weapon, InventoryController inventoryController)
        {
            if (weapon == null)
            {
                return false;
            }

            var magazineSlot = weapon.GetMagazineSlot();
            if (magazineSlot == null)
            {
                return false;
            }

            if (weapon.ReloadMode == Weapon.EReloadMode.InternalMagazine)
            {
                return false;
            }

            var compatibleMagCount = 0;
            if (weapon.GetCurrentMagazine() != null)
            {
                compatibleMagCount++;
            }

            var magazineList = new List<Magazine>();
            inventoryController.GetAcceptableItemsNonAlloc(BotReload._availableEquipmentSlots, magazineList, null, null);
            foreach (var magazine in magazineList)
            {
                if (magazineSlot.CanAccept(magazine))
                {
                    compatibleMagCount++;
                }
            }

            return compatibleMagCount < REQUIRED_MAG_COUNT;
        }

        private bool NeedAmmo(BotOwner botOwner)
        {
            var totalAmmo = CollectAllAmmoCount(botOwner);
            if (totalAmmo <= 0)
            {
                return true;
            }

            var weapon = botOwner.WeaponManager?.CurrentWeapon;
            if (weapon == null)
            {
                return false;
            }

            var magCapacity = GetMagCapacity(weapon, botOwner);
            if (magCapacity <= 0)
            {
                return false;
            }

            return totalAmmo < magCapacity * AMMO_SUFFICIENT_MULTIPLIER;
        }

        private int CollectAllAmmoCount(BotOwner botOwner)
        {
            var total = 0;
            var player = botOwner.GetPlayer;
            if (player == null)
            {
                return total;
            }

            var inventoryController = player.InventoryController;

            var allAmmoList = new List<Ammo>();
            inventoryController.GetAcceptableItemsNonAlloc(BotReload._availableEquipmentSlots, allAmmoList, null, null);
            foreach (var ammo in allAmmoList)
            {
                if (ammo != null && ammo.StackObjectsCount > 0)
                {
                    total += ammo.StackObjectsCount;
                }
            }

            var allMagList = new List<Magazine>();
            inventoryController.GetAcceptableItemsNonAlloc(BotReload._availableEquipmentSlots, allMagList, null, null);
            foreach (var magazine in allMagList)
            {
                if (magazine != null)
                {
                    total += magazine.Count;
                }
            }

            var equipment = inventoryController.Inventory.Equipment;
            foreach (var slot in new[] { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster })
            {
                if (equipment.GetSlot(slot).ContainedItem is Weapon weapon)
                {
                    total += weapon.GetCurrentMagazine()?.Count ?? 0;
                    total += weapon.ChamberAmmoCount;
                }
            }

            return total;
        }

        private int GetMagCapacity(Weapon weapon, BotOwner botOwner)
        {
            var currentMagazine = weapon.GetCurrentMagazine();
            if (currentMagazine != null && currentMagazine.MaxCount > 0)
            {
                return currentMagazine.MaxCount;
            }

            var magazineSlot = weapon.GetMagazineSlot();
            if (magazineSlot != null)
            {
                var player = botOwner.GetPlayer;
                if (player != null)
                {
                    var allMagList = new List<Magazine>();
                    player.InventoryController.GetAcceptableItemsNonAlloc(BotReload._availableEquipmentSlots, allMagList, null, null);
                    var maxCapacity = 0;
                    foreach (var magazine in allMagList)
                    {
                        if (magazine != null && magazineSlot.CanAccept(magazine) && magazine.MaxCount > maxCapacity)
                        {
                            maxCapacity = magazine.MaxCount;
                        }
                    }

                    if (maxCapacity > 0)
                    {
                        return maxCapacity;
                    }
                }
            }

            return DEFAULT_MAG_CAPACITY;
        }

        public HashSet<EDamageEffectType> GetMissingMedEffects()
        {
            _missingMedEffects.Clear();
            _missingMedEffects.Add(EDamageEffectType.Fracture);
            _missingMedEffects.Add(EDamageEffectType.HeavyBleeding);
            _missingMedEffects.Add(EDamageEffectType.LightBleeding);
            _missingMedEffects.Add(EDamageEffectType.DestroyedPart);

            var player = Player;
            if (player == null)
            {
                return _missingMedEffects;
            }

            var medsList = new List<Meds>();
            player.InventoryController.GetAcceptableItemsNonAlloc(BotMedecine.anySlots, medsList);
            foreach (var meds in medsList)
            {
                if (meds == null)
                {
                    continue;
                }

                var damageEffects = meds.HealthEffectsComponent?.DamageEffects;
                if (damageEffects == null)
                {
                    continue;
                }

                if (damageEffects.ContainsKey(EDamageEffectType.Fracture) && !damageEffects.ContainsKey(EDamageEffectType.DestroyedPart))
                {
                    _missingMedEffects.Remove(EDamageEffectType.Fracture);
                }

                if (damageEffects.ContainsKey(EDamageEffectType.HeavyBleeding))
                {
                    _missingMedEffects.Remove(EDamageEffectType.HeavyBleeding);
                }

                if (damageEffects.ContainsKey(EDamageEffectType.LightBleeding))
                {
                    _missingMedEffects.Remove(EDamageEffectType.LightBleeding);
                }

                if (damageEffects.ContainsKey(EDamageEffectType.DestroyedPart))
                {
                    _missingMedEffects.Remove(EDamageEffectType.DestroyedPart);
                }
            }

            return _missingMedEffects;
        }

        private bool TrySetEmergencyLootingTarget(List<ItemData> itemDatas, ELootNeedType needType)
        {
            if (itemDatas == null)
            {
                return false;
            }

            var botOwner = BotOwner;
            if (botOwner == null)
            {
                return false;
            }

            var weapon = botOwner.WeaponManager?.CurrentWeapon;
            if (weapon == null && needType != ELootNeedType.Meds)
            {
                return false;
            }

            var missingMedEffects = needType == ELootNeedType.Meds ? GetMissingMedEffects() : null;
            var candidateLootDatas = new List<LootData>();
            var coveredMedEffectCounts = new Dictionary<LootData, int>();

            foreach (var itemData in itemDatas)
            {
                if (itemData is not LootData lootData)
                {
                    continue;
                }

                if (lootData.IsInSecureContainerItem)
                {
                    continue;
                }

                if (lootData.RootTransform == null)
                {
                    continue;
                }

                if (LootDataMgr.IsLockedLootingTarget(lootData))
                {
                    continue;
                }

                if (LootDataMgr.IsLockedLootingTargetRootTransform(lootData.RootTransform))
                {
                    continue;
                }

                if (!lootData.LootProps.TryGetValue(McsAILeadPlayer, out var lootProp))
                {
                    continue;
                }

                if (lootProp.IsLootOnColdown(botOwner))
                {
                    continue;
                }

                switch (needType)
                {
                    case ELootNeedType.Magazine:
                        if (IsCompatibleMagazine(lootData, weapon))
                        {
                            candidateLootDatas.Add(lootData);
                        }
                        break;
                    case ELootNeedType.Ammo:
                        if (IsCompatibleAmmo(lootData, weapon))
                        {
                            candidateLootDatas.Add(lootData);
                        }
                        break;
                    case ELootNeedType.Meds:
                        {
                            var coveredCount = GetCoveredMissingMedEffectCount(lootData, missingMedEffects);
                            if (coveredCount > 0)
                            {
                                candidateLootDatas.Add(lootData);
                                coveredMedEffectCounts[lootData] = coveredCount;
                            }
                            break;
                        }
                }
            }

            if (candidateLootDatas.Count == 0)
            {
                return false;
            }

            var botPos = botOwner.Position;
            if (needType == ELootNeedType.Meds)
            {
                candidateLootDatas.Sort((a, b) =>
                {
                    var countCompare = coveredMedEffectCounts[b].CompareTo(coveredMedEffectCounts[a]);
                    if (countCompare != 0)
                    {
                        return countCompare;
                    }

                    return a.RootTransform.position.McsSqrDistance(botPos).CompareTo(b.RootTransform.position.McsSqrDistance(botPos));
                });
            }
            else if (needType == ELootNeedType.Magazine)
            {
                candidateLootDatas.Sort((a, b) =>
                {
                    var aHasAmmo = (a.Item as Magazine)?.Count > 0;
                    var bHasAmmo = (b.Item as Magazine)?.Count > 0;
                    if (aHasAmmo != bHasAmmo)
                    {
                        return aHasAmmo ? -1 : 1;
                    }

                    return a.RootTransform.position.McsSqrDistance(botPos).CompareTo(b.RootTransform.position.McsSqrDistance(botPos));
                });
            }
            else
            {
                candidateLootDatas.Sort((a, b) => a.RootTransform.position.McsSqrDistance(botPos).CompareTo(b.RootTransform.position.McsSqrDistance(botPos)));
            }

            var targetLootData = candidateLootDatas.FirstOrDefault();
            LootDataMgr.LockLootItemToTarget(targetLootData);
            LootDataMgr.LockLootingTargetRootTransform(targetLootData.RootTransform);
            LootingTarget = targetLootData;
            return true;
        }

        private bool IsCompatibleMagazine(LootData lootData, Weapon weapon)
        {
            if (lootData.Item is not Magazine magazine)
            {
                return false;
            }

            var magazineSlot = weapon.GetMagazineSlot();
            if (magazineSlot == null)
            {
                return false;
            }

            return magazineSlot.CanAccept(magazine);
        }

        private bool IsCompatibleAmmo(LootData lootData, Weapon weapon)
        {
            if (lootData.Item is not Ammo ammo)
            {
                return false;
            }

            if (weapon.Chambers != null)
            {
                foreach (var chamber in weapon.Chambers)
                {
                    if (chamber != null && chamber.CanAccept(ammo))
                    {
                        return true;
                    }
                }
            }

            var currentMagazine = weapon.GetCurrentMagazine();
            if (currentMagazine?.Cartridges?.Filters != null && currentMagazine.Cartridges.Filters.CheckItemFilter(ammo))
            {
                return true;
            }

            return false;
        }

        private int GetCoveredMissingMedEffectCount(LootData lootData, HashSet<EDamageEffectType> missingMedEffects)
        {
            if (lootData.Item is not Meds meds)
            {
                return 0;
            }

            var damageEffects = meds.HealthEffectsComponent?.DamageEffects;
            if (damageEffects == null)
            {
                return 0;
            }

            var coveredCount = 0;
            foreach (var missingEffect in missingMedEffects)
            {
                switch (missingEffect)
                {
                    case EDamageEffectType.Fracture:
                        if (damageEffects.ContainsKey(EDamageEffectType.Fracture) && !damageEffects.ContainsKey(EDamageEffectType.DestroyedPart))
                        {
                            coveredCount++;
                        }
                        break;
                    default:
                        if (damageEffects.ContainsKey(missingEffect))
                        {
                            coveredCount++;
                        }
                        break;
                }
            }

            return coveredCount;
        }
    }
}