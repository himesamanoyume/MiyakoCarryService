
using System.Collections.Generic;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class ProfileController(
        ProfileService profileService
    )
    {
        public void ProcessExpiredMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            profileService.ProcessExpiredMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
        }

        public void ProcessExpiredMcsBotPlayerProfiles(MongoId mcsLeadPlayerId, HashSet<MongoId> mcsBotPlayerIds)
        {
            profileService.ProcessExpiredMcsBotPlayerProfiles(mcsLeadPlayerId, mcsBotPlayerIds);
        }

        public async Task SaveMcsBotPlayerProfile(MongoId mcsLeadPlayerId, SptProfile mcsBotPlayerProfile)
        {
            await profileService.SaveMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerProfile);
        }

        public SptProfile Generate(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId, PmcData completeQuestPmcData, SpawnType botType, int carryServiceLevel)
        {
            return profileService.Generate(mcsLeadPlayerId, mcsBotPlayerId, completeQuestPmcData, botType, carryServiceLevel);
        }

        public SptProfile? GetMcsBotPlayerProfile(MongoId mcsLeadPlayerId, MongoId mcsBotPlayerId)
        {
            return profileService.GetMcsBotPlayerProfile(mcsLeadPlayerId, mcsBotPlayerId);
        }

        public SptProfile? GetMcsBotPlayerProfileByAccountId(MongoId mcsLeadPlayerId, string mcsAid)
        {
            return profileService.GetMcsBotPlayerProfileByAccountId(mcsLeadPlayerId, mcsAid);
        }

        public SptProfile? GetMcsBotPlayerProfileByBotId(MongoId mcsBotPlayerId)
        {
            return profileService.GetMcsBotPlayerProfileByBotId( mcsBotPlayerId);
        }

        public List<SptProfile> GetAllMcsBotPlayerProfileByBossId(MongoId mcsLeadPlayerId)
        {
            return profileService.GetAllMcsBotPlayerProfileByBossId(mcsLeadPlayerId);
        }
    }
}