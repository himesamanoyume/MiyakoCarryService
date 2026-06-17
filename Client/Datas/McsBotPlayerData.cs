

using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
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
        public GamePlayerOwner LeadPlayerGamePlayerOwner => McsAILeadPlayer.GamePlayerOwner;
        private WeakReference<McsAILeadPlayer> _mcsAILeadPlayerRef;
        public McsAILeadPlayer McsAILeadPlayer => _mcsAILeadPlayerRef.TryGetTarget(out var mcsAILeadPlayer) ? mcsAILeadPlayer : null;
        private LootData _lootingTarget = null;
        private bool _isLooting = false;
        public Vector3? EscortPos = null;
        public LootData LootingTarget
        {
            get => _lootingTarget;
            set => _lootingTarget = value;
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
        public bool IsTaskRunning = false;
        private EDecision _decision = EDecision.None;

        public void SetDecision(EDecision[] exclude = null, params EDecision[] decisions)
        {
            var preserved = EDecision.None;
            if (exclude != null)
            {
                foreach (var e in exclude)
                {
                    if (_decision.HasFlag(e))
                    {
                        preserved |= e;
                    }
                }
            }

            _decision = EDecision.None;
            foreach (var decision in decisions)
            {
                _decision |= decision;
            }

            _decision |= preserved;
        }

        public bool HasDecision(params EDecision[] decisions)
        {
            foreach (var decision in decisions)
            {
                if (!_decision.HasFlag(decision))
                {
                    return false;
                }
            }
            return true;
        }

        public McsBotPlayerData(Player bossPlayer, McsAILeadPlayer mcsAILeadPlayer, Player player, Item item) : base(player, item)
        {
            _botOwnerRef = new(player.AIData.BotOwner);
            BotOwner.SetMcsBotPlayerData(this);
            _mcsAILeadPlayerRef = new(mcsAILeadPlayer);
            _leadPlayeRef = new(bossPlayer);
        }

        public void SetLootingTarget(List<ItemData> itemDatas)
        {
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
                if (_lootDataMgr.IsLockedLootingTarget(containerData))
                {
                    continue;
                }

                if (_lootDataMgr.IsLockedLootingTargetRootTransform(containerData.RootTransform))
                {
                    continue;
                }

                _lootDataMgr.LockLootItemToTarget(containerData);
                _lootDataMgr.LockLootingTargetRootTransform(containerData.RootTransform);
                LootingTarget = containerData;
                return;
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
        }

        public override void Dispose()
        {
            base.Dispose();
            _botOwnerRef = null;
            _leadPlayeRef = null;
            _mcsAILeadPlayerRef = null;
            _isLooting = false;
            _lootingTarget = null;
        }
    }
}