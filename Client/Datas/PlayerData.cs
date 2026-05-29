

using System;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class PlayerData : ItemData
    {
        private WeakReference<Player> _playerRef;
        public Player Player
        {
            get
            {
                try
                {
                    if (_playerRef == null)
                    {
                        return null;
                    }
                    return _playerRef.TryGetTarget(out var player) ? player : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public PlayerData(Player player, Item item) : base(item)
        {
            _playerRef = new(player);
        }

        public override void RefreshInteresting(McsAILeadPlayer mcsAILeadPlayer)
        {
            if (ItemsInContainer == null)
            {
                ItemsInContainer = Item.GetAllDatas().ToList();
            }

            foreach (var itemData in ItemsInContainer)
            {
                if (itemData == null)
                {
                    continue;
                }

                if (itemData.Item.Id == Item.Id)
                {
                    continue;
                }

                if (this == itemData)
                {
                    continue;
                }

                if (itemData is not LootData lootData)
                {
                    continue;
                }

                lootData.Refresh(mcsAILeadPlayer);
            }
        }

        public override void UnlockRefreshInteresting(McsAILeadPlayer mcsAILeadPlayer)
        {
            _lootDataMgr.UnlockLootingTargetRootTransform(RootTransform);
            if (ItemsInContainer == null)
            {
                ItemsInContainer = Item.GetAllDatas().ToList();
            }

            foreach (var itemData in ItemsInContainer)
            {
                if (itemData == null)
                {
                    continue;
                }

                if (itemData.Item.Id == Item.Id)
                {
                    continue;
                }

                if (this == itemData)
                {
                    continue;
                }

                if (itemData is not LootData lootData)
                {
                    continue;
                }

                _lootDataMgr.UnlockLootingTarget(lootData);
                lootData.Refresh(mcsAILeadPlayer);
            }
        }

        protected override Transform GetRootTransfrom()
        {
            try
            {
                var transform = Player?.PlayerBones?.Ribcage?.Original;
                if (transform == null)
                {
                    return null;
                }
                return transform;
            }
            catch
            {
                return null;
            }
        }
    }
}