
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT;
using MiyakoCarryService.Client.Bots.BotBehaviors;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Mgrs;

namespace MiyakoCarryService.Client.Extensions
{
    internal static class BotOwnerExtensions
    {
        private static readonly ConditionalWeakTable<BotOwner, IEnumerable<BotBehavior>> _botBehaviorDict = new();
        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>(EMgrType.SQUAD);
            }
        }
        
        extension(BotOwner botOwner)
        {
            public IEnumerable<BotBehavior> GetBotBehaviors()
            {
                return _botBehaviorDict.TryGetValue(botOwner, out var botBehaviors) ? botBehaviors : botOwner.InitBotBehaviors();
            }

            public IEnumerable<BotBehavior> InitBotBehaviors()
            {
                var mcsBossPlayer = SquadMgr.GetMcsBossPlayer(botOwner.ProfileId);
                return [new BotCarryServiceChecker(botOwner, mcsBossPlayer)];
            }
        }
    }
}
