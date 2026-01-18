

using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Bots.BotBehaviors;

namespace MiyakoCarryService.Client.Datas
{
    internal sealed class McsPlayerData : PlayerData
    {
        private WeakReference<BotOwner> _botOwnerRef;
        public BotOwner BotOwner
        {
            get
            {
                _botOwnerRef.TryGetTarget(out var botOwner);
                return botOwner;
            }
        }
        private WeakReference<Player> _bossPlayeRef;
        public Player BossPlayer
        {
            get
            {
                _bossPlayeRef.TryGetTarget(out var bossPlayer);
                return bossPlayer;
            }
        }
        public List<BotBehavior> BotBehaviors { get; private set; }
        public McsPlayerData(Player bossPlayer, Player player, Item item) : base(player, item)
        {
            _botOwnerRef = new(player.AIData.BotOwner);
            _bossPlayeRef = new(bossPlayer);
            BotBehaviors = [new BotCarryServiceChecker(BotOwner, BossPlayer)];
        }
    }
}