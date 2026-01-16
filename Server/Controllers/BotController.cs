
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
        ProfileController profileController,
        CompatibilityService compatibilityService
    )
    {
        public async Task<Dictionary<MongoId, IEnumerable<PmcData>>> SpawnCarryServicePlayer(MongoId sessionId)
        {
            var bossSessionIds = new HashSet<MongoId> { sessionId };

            if (compatibilityService.HasFikaServer)
            {
                // 暂不进行验证
                var fikaMatchService = compatibilityService.FikaMatchService;
                var matchId = (MongoId?)AccessTools.Method(fikaMatchService, "GetMatchIdByPlayer")?.Invoke(null, [sessionId]);

                if (matchId is not null)
                {
                    var fikaMatch = AccessTools.Method(fikaMatchService, "GetMatch")?.Invoke(null, [matchId]);

                    if (fikaMatch is not null)
                    {
                        var fikaPlayers = (Dictionary<MongoId, object>)AccessTools.Property(compatibilityService.FikaMatch, "Players")?.GetValue(fikaMatch);

                        foreach (var playerSessionId in fikaPlayers.Keys)
                        {
                            if (playerSessionId != sessionId)
                            {
                                bossSessionIds.Add(playerSessionId);
                            }
                        }
                    }
                }
            }

            var tasks = bossSessionIds.Select(async bossSessionId =>
            {
                var pmcDatas = await Task.Run(() =>
                {
                    var profiles = profileController.GetCSFullProfileByBossId(bossSessionId);
                    return profiles.Select(p => p.CharacterData.PmcData).ToList();
                });

                return new KeyValuePair<MongoId, IEnumerable<PmcData>>(bossSessionId, pmcDatas);
            });

            var results = await Task.WhenAll(tasks);

            var csPmcDatas = results.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );
            
            return csPmcDatas;
        }
    }
}