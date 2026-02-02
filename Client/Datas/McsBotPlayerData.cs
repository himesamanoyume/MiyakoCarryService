

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
            try
            {
                if (LootingTarget != null)
                {
                    IsRunningCoroutine = true;
                    // 模拟打开容器的时间
                    yield return new WaitForSeconds(2f);
                    BotOwner.ShowSubtitleMsg(string.Format("<b>{0}</b>:到达战利品位置, 这里有{1}".McsLocalized(), BotOwner.Profile.Nickname, LootingTarget.Item.ShortName.McsLocalized()));
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