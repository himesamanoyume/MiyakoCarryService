

using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class PlayerData : ItemData
    {
        private Player _playerRef;
        public Player Player => _playerRef;

        public PlayerData(Player player, Item item) : base(item)
        {
            _playerRef = player;
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

        public override void Dispose()
        {
            base.Dispose();
            _playerRef = null;
        }
    }
}