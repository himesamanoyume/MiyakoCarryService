

using System;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    internal class PlayerData : ItemData
    {
        private WeakReference<Player> _playerRef;
        public Player Player => _playerRef.TryGetTarget(out var player) ? player : null;

        public PlayerData(Player player, Item item) : base(item)
        {
            _playerRef = new(player);
        }

        public override void UpdateAllLootInContainerInfo(McsAIBossPlayer mcsAIBossPlayer)
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

                lootData.Refresh(mcsAIBossPlayer);
            }
        }

        protected override Transform GetTransfrom()
        {
            try
            {
                return Player.PlayerBones.Ribcage.Original;
            }
            catch
            {
                return null;
            }
        }
    }
}