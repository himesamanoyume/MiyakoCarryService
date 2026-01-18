
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
        public void AddGroupMember(MongoId mcsBossPlayerId, int mcsAid)
        {
            raidService.AddGroupMember(mcsBossPlayerId, mcsAid);
        }

        public void RemoveGroupMember(MongoId mcsBossPlayerId, int mcsAid)
        {
            raidService.RemoveGroupMember(mcsBossPlayerId, mcsAid);
        }

        public void ClearGroupMember(MongoId mcsBossPlayerId)
        {
            raidService.ClearGroupMember(mcsBossPlayerId);
        }

        public bool CheckCSPlayerExist(MongoId mcsBossPlayerId, int mcsAid)
        {
            return raidService.CheckCSPlayerExist(mcsBossPlayerId, mcsAid);
        }

        public void AcceptGroupInvite(MongoId mcsBossPlayerId, int mcsAid)
        {
            raidService.AcceptGroupInvite(mcsBossPlayerId, mcsAid);
        }
    }
}