
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT;
using MiyakoCarryService.Client.BotBehaviors;

namespace MiyakoCarryService.Client.Extensions
{
    internal static class BotOwnerExtensions
    {
        private static readonly ConditionalWeakTable<BotOwner, IEnumerable<BotBehavior>> _botBehaviorDict = new();
        
        extension(BotOwner botOwner)
        {
            public IEnumerable<BotBehavior> GetBotBehaviors()
            {
                return _botBehaviorDict.TryGetValue(botOwner, out var botBehaviors) ? botBehaviors : botOwner.InitBotBehaviors();
            }

            public IEnumerable<BotBehavior> InitBotBehaviors()
            {
                return [new BotCarryServiceChecker(botOwner)];
            }
        }
    }
}
