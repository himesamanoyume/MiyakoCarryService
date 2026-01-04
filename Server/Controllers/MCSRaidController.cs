
using MiyakoCarryService.Server.Helper;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;

namespace MiyakoCarryService.Server.Controllers
{
    [Injectable]
    public class MCSRaidController(
        MCSRaidService mcsRaidService
    )
    {
        public void AddGroupMember(MongoId bossSessionId, int csAid)
        {
            mcsRaidService.AddGroupMember(bossSessionId, csAid);
        }

        public void RemoveGroupMember(MongoId bossSessionId, int csAid)
        {
            mcsRaidService.RemoveGroupMember(bossSessionId, csAid);
        }

        public bool CheckCSPlayerExist(MongoId bossSessionId, int csAid)
        {
            return mcsRaidService.CheckCSPlayerExist(bossSessionId, csAid);
        }

        public void AcceptGroupInvite(MongoId bossSessionId, int csAid)
        {
            mcsRaidService.AcceptGroupInvite(bossSessionId, csAid);
        }
    }
}