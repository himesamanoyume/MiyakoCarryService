
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class BotController(
        RaidController raidController,
        CompatibilityService compatibilityService
    )
    {
        public async Task<Dictionary<MongoId, IEnumerable<PmcData>>> SpawnMcsBotPlayer(MongoId mcsBossPlayerId)
        {
            var mcsBossPlayerIds = new HashSet<MongoId> { mcsBossPlayerId };

            if (compatibilityService.HasFikaServer)
            {
                // 暂不进行验证
                var fikaMatchService = compatibilityService.FikaMatchService;
                var matchId = (MongoId?)AccessTools.Method(fikaMatchService, "GetMatchIdByPlayer")?.Invoke(null, [mcsBossPlayerId]);

                if (matchId is not null)
                {
                    var fikaMatch = AccessTools.Method(fikaMatchService, "GetMatch")?.Invoke(null, [matchId]);

                    if (fikaMatch is not null)
                    {
                        var fikaPlayers = (Dictionary<MongoId, object>)AccessTools.Property(compatibilityService.FikaMatch, "Players")?.GetValue(fikaMatch);

                        foreach (var playerId in fikaPlayers.Keys)
                        {
                            if (playerId != mcsBossPlayerId)
                            {
                                mcsBossPlayerIds.Add(playerId);
                            }
                        }
                    }
                }
            }

            var tasks = mcsBossPlayerIds.Select(async mcsBossPlayerId =>
            {
                var pmcDatas = await Task.Run(() =>
                {
                    var profiles = raidController.GetAllGroupMemberProfiles(mcsBossPlayerId);
                    raidController.ClearGroupMember(mcsBossPlayerId);
                    return profiles.Select(p => p.CharacterData.PmcData).ToList();
                });

                return new KeyValuePair<MongoId, IEnumerable<PmcData>>(mcsBossPlayerId, pmcDatas);
            });

            var results = await Task.WhenAll(tasks);

            var mcsPmcDatas = results.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );
            
            return mcsPmcDatas;
        }
    }
}