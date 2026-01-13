
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
        public async Task<IEnumerable<PmcData>> SpawnCarryServicePlayer(MongoId sessionId)
        {
            var sessionIds = new HashSet<MongoId> { sessionId };

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
                                sessionIds.Add(playerSessionId);
                            }
                        }
                    }
                }
            }

            var tasks = sessionIds.Select(id => Task.Run(() =>
            {
                var profiles = profileController.GetCSFullProfileByBossId(id);
                return profiles.Select(p => p.CharacterData.PmcData).ToList();
            })).ToList();

            var results = await Task.WhenAll(tasks);
            var csPmcDatas = results.SelectMany(list => list);
            return csPmcDatas;
        }
    }
}