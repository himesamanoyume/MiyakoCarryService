

using System;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Models;

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

        public override void UpdateAllLootInContainerInfo(McsBotPlayerConfig mcsBotPlayerConfig)
        {
            throw new NotImplementedException();
        }
    }
}