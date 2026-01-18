

using System;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Datas
{
    internal class PlayerData : ItemData
    {
        private WeakReference<Player> _playerRef;
        public Player Player
        {
            get
            {
                _playerRef.TryGetTarget(out var player);
                return player;
            }
        }

        public EStance Stance = EStance.Enemy;

        public PlayerData(Player player, Item item) : base(item)
        {
            _playerRef = new(player);
        }

        public override void UpdateAllLootInContainerInfo()
        {
            throw new NotImplementedException();
        }
    }
}