

using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Bots.BotBehaviors;
using MiyakoCarryService.Client.Misc;

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
        public ItemData LootingTarget = null;
        public McsBotPlayerData(Player bossPlayer, McsAIBossPlayer mcsAIBossPlayer, Player player, Item item) : base(player, item)
        {
            _botOwnerRef = new(player.AIData.BotOwner);
            _mcsAIBossPlayerRef = new(mcsAIBossPlayer);
            _bossPlayeRef = new(bossPlayer);
            BotBehaviors = [new BotCarryServiceChecker(BotOwner, BossPlayer)];
        }

        public void SetLootingTarget(List<ItemData> itemDatas)
        {
            var filtedItemDatas = new List<LootData>(itemDatas.Count);
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

                if (!lootData.LootProps.TryGetValue(McsAIBossPlayer, out var lootProp))
                {
                    continue;
                }

                if (lootProp.IsBlockItem)
                {
                    continue;
                }

                // 此处不够完善，比如如果一个战利品是任务物品，但价值很低，会导致其优先级很低，后续仍需完善
                if (lootProp.IsWishListItem || lootProp.IsHighPriceItem || lootProp.IsQuestNeedItem)
                {
                    filtedItemDatas.Add(lootData);
                }
            }

            filtedItemDatas.Sort((a, b) => b.Offer.Price.CompareTo(a.Offer.Price));
            foreach (var lootData in filtedItemDatas)
            {
                if (!lootData.IsLootingTarget)
                {
                    lootData.IsLootingTarget = true;
                    LootingTarget = lootData;
                    break;
                }
            }
        }

        private bool WeaponFilter()
        {
            return true;
        }

        private bool EquipmentFilter()
        {
            return true;
        }

        private bool MedecineFilter()
        {
            return true;
        }

        private bool FoodFilter()
        {
            return true;
        }

    }
}