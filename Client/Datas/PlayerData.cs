

using System;
using EFT;
using EFT.InventoryLogic;
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