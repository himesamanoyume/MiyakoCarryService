
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Extensions
{
    public static class BotOwnerExtensions
    {
        private static PlayerDataMgr PlayerDataMgr => MgrAccessor.Get<PlayerDataMgr>();

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        private static SubtitlesMgr SubtitlesMgr => MgrAccessor.Get<SubtitlesMgr>();

        private static readonly ConditionalWeakTable<BotOwner, McsBotPlayerData> _datas = new();

        extension(BotOwner botOwner)
        {
            public bool IsMcsBotPlayer => McsMgr.IsMcsBotPlayer(botOwner.ProfileId);

            public McsBotPlayerData GetMcsBotPlayerData()
            {
                if (_datas.TryGetValue(botOwner, out var mcsBotPlayerData))
                {
                    return mcsBotPlayerData;
                }

                var mcsBotPlayerDatas = PlayerDataMgr.GetMcsBotPlayerDatas();
                foreach (var _mcsBotPlayerData in mcsBotPlayerDatas)
                {
                    if (_mcsBotPlayerData.BotOwner == botOwner)
                    {
                        _datas.Add(botOwner, _mcsBotPlayerData);
                        return _mcsBotPlayerData;
                    }
                }
                return null;
            }

            public void SetMcsBotPlayerData(McsBotPlayerData mcsBotPlayerData)
            {
                _datas.AddOrUpdate(botOwner, mcsBotPlayerData);
            }

            public void TalkMsg(McsMsg msg)
            {
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData == null)
                {
                    return;
                }
                SubtitlesMgr.TalkMsg(mcsBotPlayerData.LeadPlayer, mcsBotPlayerData.Player, msg);
            }

            public void TalkMsg(Player mcsLeadPlayer, Player mcsBotPlayer, McsMsg msg)
            {
                SubtitlesMgr.TalkMsg(mcsLeadPlayer, mcsBotPlayer, msg);
            }

            public Vector3 GetMcsLeadPlayerPos(McsBotPlayerData mcsBotPlayerData)
            {
                if (mcsBotPlayerData != null && mcsBotPlayerData.LeadPlayer != null && mcsBotPlayerData.LeadPlayer.HealthController.IsAlive)
                {
                    return mcsBotPlayerData.LeadPlayer.Position;
                }
                else if (botOwner.BotFollower.HaveBoss)
                {
                    return botOwner.BotFollower.BossToFollow.Position;
                }
                else
                {
                    if (botOwner.Position == null)
                    {
                        return new();
                    }
                    return botOwner.Position;
                }
            }

            public void TryChangeWeaponSlot(EquipmentSlot slot)
            {
                var weaponManager = botOwner.WeaponManager;
                if (weaponManager?.Selector == null)
                {
                    return;
                }

                switch (slot)
                {
                    case EquipmentSlot.FirstPrimaryWeapon:
                        weaponManager.Selector.ChangeToMain();
                        break;
                    case EquipmentSlot.SecondPrimaryWeapon:
                        weaponManager.Selector.ChangeToSecond();
                        break;
                    case EquipmentSlot.Holster:
                        weaponManager.Selector.TryChangeToSlot(slot, false);
                        break;
                    case EquipmentSlot.Scabbard:
                        if (weaponManager.Selector.CanChangeToMeleeWeapons)
                        {
                            weaponManager.Selector.ChangeToMelee();
                        }
                        break;
                }
            }

            public bool HasBackupAmmo(Weapon weapon, out int total)
            {
                total = 0;
                var player = botOwner.GetPlayer;
                var inventoryController = player.InventoryController;

                var currentMagazine = weapon.GetCurrentMagazine();
                var magazineSlot = weapon.GetMagazineSlot();

                if (magazineSlot != null)
                {
                    var preallocatedMagList = new List<Magazine>();
                    inventoryController.GetReachableItemsOfTypeNonAlloc(preallocatedMagList, null);

                    var hasUnusedMagazine = false;

                    foreach (var mag in preallocatedMagList)
                    {
                        if (mag == currentMagazine)
                        {
                            continue;
                        }

                        if (magazineSlot.CanAccept(mag))
                        {
                            hasUnusedMagazine = true;
                            if (mag.Count > 0)
                            {
                                total += mag.Count;
                                return true;
                            }
                        }
                    }

                    if (hasUnusedMagazine)
                    {
                        if (botOwner.HasLooseAmmoForWeapon(weapon, out var _total))
                        {
                            total += _total;
                            return true;
                        }
                    }
                }

                if (currentMagazine == null)
                {
                    var flag = botOwner.HasLooseAmmoForWeapon(weapon, out var _total);
                    total += _total;
                    return flag;
                }

                return false;
            }

            public bool HasLooseAmmoForWeapon(Weapon weapon, out int total)
            {
                total = 0;
                var player = botOwner.GetPlayer;
                var inventoryController = player.InventoryController;

                var chamberSlot = weapon.HasChambers ? weapon.Chambers[0] : null;
                var preallocatedAmmoList = new List<Ammo>();
                inventoryController.GetAcceptableItemsNonAlloc(
                    BotReload._availableEquipmentSlots,
                    preallocatedAmmoList,
                    null,
                    null
                );

                foreach (var ammo in preallocatedAmmoList)
                {
                    if (ammo.StackObjectsCount > 0)
                    {
                        if (chamberSlot != null && chamberSlot.CanAccept(ammo))
                        {
                            total += ammo.StackObjectsCount;
                            return true;
                        }

                        if (weapon.GetCurrentMagazine() != null)
                        {
                            var currentMag = weapon.GetCurrentMagazine();
                            if (currentMag.Cartridges.Filters.CheckItemFilter(ammo))
                            {
                                total += ammo.StackObjectsCount;
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            public bool HasAmmoOrBackupAmmo(EquipmentSlot slot, out int total)
            {
                total = 0;
                var equipment = botOwner.GetPlayer.InventoryController.Inventory.Equipment;

                if (!equipment.HasWeaponInSlot(slot))
                {
                    return false;
                }

                var item = equipment.GetSlot(slot).ContainedItem;
                if (item is not Weapon weapon)
                {
                    return false;
                }

                var magazineSlot = weapon.GetMagazineSlot();
                if (magazineSlot?.ContainedItem is Magazine magazine)
                {
                    if (magazine.Count > 0)
                    {
                        total += magazine.Count;
                        return true;
                    }
                }

                if (weapon.ChamberAmmoCount > 0)
                {
                    total += weapon.ChamberAmmoCount;
                    return true;
                }

                var has = botOwner.HasBackupAmmo(weapon, out var _total);
                total += _total;
                return has;
            }

            public EquipmentSlot DetermineWeaponSlotByAmmo(EquipmentSlot currentSlot, out int total)
            {
                total = 0;
                var equipment = botOwner.GetPlayer.InventoryController.Inventory.Equipment;

                if (equipment.HasWeaponInSlot(EquipmentSlot.FirstPrimaryWeapon))
                {
                    if (botOwner.HasAmmoOrBackupAmmo(EquipmentSlot.FirstPrimaryWeapon, out var _total))
                    {
                        total += _total;
                        return EquipmentSlot.FirstPrimaryWeapon;
                    }
                }

                if (equipment.HasWeaponInSlot(EquipmentSlot.SecondPrimaryWeapon))
                {
                    if (botOwner.HasAmmoOrBackupAmmo(EquipmentSlot.SecondPrimaryWeapon, out var _total))
                    {
                        total += _total;
                        return EquipmentSlot.SecondPrimaryWeapon;
                    }
                }

                if (equipment.HasWeaponInSlot(EquipmentSlot.Holster))
                {
                    if (botOwner.HasAmmoOrBackupAmmo(EquipmentSlot.Holster, out var _total))
                    {
                        total += _total;
                        return EquipmentSlot.Holster;
                    }
                }

                if (equipment.HasKnifeInSlot(EquipmentSlot.Scabbard))
                {
                    return EquipmentSlot.Scabbard;
                }

                return currentSlot;
            }

            public void CollectAmmoOrBackupAmmoCount(out int total)
            {
                total = 0;
                var equipment = botOwner.GetPlayer.InventoryController.Inventory.Equipment;

                if (equipment.HasWeaponInSlot(EquipmentSlot.FirstPrimaryWeapon))
                {
                    if (botOwner.HasAmmoOrBackupAmmo(EquipmentSlot.FirstPrimaryWeapon, out var _total))
                    {
                        total += _total;
                    }
                }

                if (equipment.HasWeaponInSlot(EquipmentSlot.SecondPrimaryWeapon))
                {
                    if (botOwner.HasAmmoOrBackupAmmo(EquipmentSlot.SecondPrimaryWeapon, out var _total))
                    {
                        total += _total;
                    }
                }

                if (equipment.HasWeaponInSlot(EquipmentSlot.Holster))
                {
                    if (botOwner.HasAmmoOrBackupAmmo(EquipmentSlot.Holster, out var _total))
                    {
                        total += _total;
                    }
                }
            }

            public float McsGetCurrentMagAmmoRatio()
            {
                var selector = botOwner.WeaponManager?.Selector;
                if (selector == null)
                {
                    return 1f;
                }

                var equipment = botOwner.GetPlayer.InventoryController.Inventory.Equipment;
                var slot = selector.EquipmentSlot;

                if (!equipment.HasWeaponInSlot(slot))
                {
                    return 1f;
                }

                if (equipment.GetSlot(slot).ContainedItem is not Weapon weapon)
                {
                    return 1f;
                }

                var magazine = weapon.GetCurrentMagazine();
                if (magazine == null)
                {
                    return 1f;
                }

                var maxCount = magazine.MaxCount;
                if (maxCount <= 0)
                {
                    return 1f;
                }

                var current = magazine.Count + weapon.ChamberAmmoCount;
                return (float)current / (maxCount + weapon.ChamberAmmoCount);
            }

            public void TryResetHandsState()
            {
                var player = botOwner.GetPlayer;
                if (player?.HandsController == null)
                {
                    return;
                }

                var handsIdle = !player.HandsController.IsAiming
                        && !player.HandsController.IsInventoryOpen()
                        && !player.HandsController.IsInInteractionStrictCheck()
                        && !player.HandsController.IsHandsProcessing();

                botOwner.Mover.AllowTeleport();
                botOwner.Mover.LastGoodCastPoint = botOwner.Mover.PrevSuccessLinkedFrom_1 = botOwner.Mover.PrevLinkPos = botOwner.Mover.PositionOnWayInner = botOwner.Position;
                botOwner.Mover.SetPlayerToNavMesh(botOwner.Position);

                if (!botOwner.Medecine.Using && handsIdle)
                {
                    if (botOwner.Medecine.FirstAid.Using)
                    {
                        botOwner.Medecine.FirstAid.CancelCurrent();
                    }
                    if (botOwner.Medecine.SurgicalKit.Using)
                    {
                        botOwner.Medecine.SurgicalKit.CancelCurrent();
                    }
                    if (botOwner.Medecine.Stimulators.Using)
                    {
                        botOwner.Medecine.Stimulators.CancelCurrent();
                    }
                    player.FastForwardCurrentOperations();
                    player.SetInventoryOpened(false);
                    if (botOwner.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.FirstPrimaryWeapon)
                    {
                        botOwner.WeaponManager.Selector.TryChangeToMain();
                    }
                    else
                    {
                        player.TrySetLastEquippedWeapon();
                    }
                }
                botOwner.Mover.LastGoodCastPointTime = Time.time;
                botOwner.Mover.PrevPosLinkedTime_1 = 0f;
                botOwner.Mover.RecalcWay();
                botOwner.Mover.Pause = true;
#if DEBUG
                MiyakoCarryServicePlugin.Logger.LogWarning("尝试强制重置手部状态" + Time.time);
#endif
            }
        }
    }
}