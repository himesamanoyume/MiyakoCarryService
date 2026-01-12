
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class RaidController(
        RaidService raidService
    )
    {
        public void AddGroupMember(MongoId bossSessionId, int csAid)
        {
            raidService.AddGroupMember(bossSessionId, csAid);
        }

        public void RemoveGroupMember(MongoId bossSessionId, int csAid)
        {
            raidService.RemoveGroupMember(bossSessionId, csAid);
        }

        public void ClearGroupMember(MongoId bossSessionId)
        {
            raidService.ClearGroupMember(bossSessionId);
        }

        public bool CheckCSPlayerExist(MongoId bossSessionId, int csAid)
        {
            return raidService.CheckCSPlayerExist(bossSessionId, csAid);
        }

        public void AcceptGroupInvite(MongoId bossSessionId, int csAid)
        {
            raidService.AcceptGroupInvite(bossSessionId, csAid);
        }
    }
}