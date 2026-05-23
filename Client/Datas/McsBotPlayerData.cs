

using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Bots.BotBehaviors;
using MiyakoCarryService.Client.Misc;

namespace MiyakoCarryService.Client.Datas
{
    public sealed class McsBotPlayerData : PlayerData
    {
        private WeakReference<BotOwner> _botOwnerRef;
        public BotOwner BotOwner => _botOwnerRef.TryGetTarget(out var botOwner) ? botOwner : null;
        private WeakReference<Player> _leadPlayeRef;
        public Player LeadPlayer => _leadPlayeRef.TryGetTarget(out var leadPlayer) ? leadPlayer : null;
        public List<BotBehavior> BotBehaviors { get; private set; }
        public GamePlayerOwner LeadPlayerGamePlayerOwner => McsAILeadPlayer.GamePlayerOwner;
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
        public bool IsTaskRunning = false;
        public bool ShouldHoldPosition = false;
        public bool ShouldGoToPoint = false;

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

                if (!lootData.LootProps.TryGetValue(McsAILeadPlayer, out var lootProp))
                {
                    continue;
                }

                if (lootProp.IsBlockItem)
                {
                    continue;
                }

                if (!lootProp.IsHighPriceItem && (!McsAILeadPlayer.McsBotPlayerConfig.LootingKeywordItem || !lootProp.IsKeywordItem))
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
    }
}