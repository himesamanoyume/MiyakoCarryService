
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class RaidController(
        RaidService raidService
    )
    {
        public void AddGroupMember(MongoId mcsLeadPlayerId, int mcsAid)
        {
            raidService.AddGroupMember(mcsLeadPlayerId, mcsAid);
        }

        public void RemoveGroupMember(MongoId mcsLeadPlayerId, int mcsAid)
        {
            raidService.RemoveGroupMember(mcsLeadPlayerId, mcsAid);
        }

        public void ClearGroupMember(MongoId mcsLeadPlayerId)
        {
            raidService.ClearGroupMember(mcsLeadPlayerId);
        }

        public bool CheckMcsBotPlayerExist(MongoId mcsLeadPlayerId, int mcsAid)
        {
            return raidService.CheckMcsBotPlayerExist(mcsLeadPlayerId, mcsAid);
        }

        public void AcceptGroupInvite(MongoId mcsLeadPlayerId, int mcsAid)
        {
            raidService.AcceptGroupInvite(mcsLeadPlayerId, mcsAid);
        }

        public List<SptProfile> GetAllGroupMemberProfiles(MongoId mcsLeadPlayerId)
        {
            return raidService.GetAllGroupMemberProfiles(mcsLeadPlayerId);
        }

        public async Task<Dictionary<MongoId, IEnumerable<PmcData>>> SpawnMcsBotPlayer(MongoId mcsLeadPlayerId, SideType side)
        {
            return await raidService.SpawnMcsBotPlayer(mcsLeadPlayerId, side);
        }

        public async Task<List<MongoId>> GetAllMcsBotPlayerIdInRaid(MongoId mcsLeadPlayerId, SideType side)
        {
            return await raidService.GetAllMcsBotPlayerIdInRaid(mcsLeadPlayerId, side);
        }
    
        public async Task<Dictionary<MongoId, McsBotPlayerConfigRequestData>> GetMcsBotPlayerConfigs(MongoId mcsLeadPlayerId)
        {
            return await raidService.GetMcsBotPlayerConfigs(mcsLeadPlayerId);
        }

        public List<MongoId> GetMcsBotPlayerIds(MongoId mcsLeadPlayerId, SideType side)
        {
            return raidService.GetMcsBotPlayerIds(mcsLeadPlayerId, side).ToList();
        }

        public async Task CollectMcsBotPlayerConfig(McsBotPlayerConfigRequestData mcsBotPlayerConfig)
        {
            await raidService.CollectMcsBotPlayerConfig(mcsBotPlayerConfig);
        }
    }
}