using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Helper;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class RaidService(
        NotificationHelper notificationHelper,
        NotificationSendHelper notificationSendHelper,
        ProfileService profileService
    )
    {
        private readonly ConcurrentDictionary<MongoId, List<int>> _bossMemberGroups = new();

        public async Task OnPostLoadAsync()
        {

        }

        public bool CheckMcsPlayerExist(MongoId mcsBossPlayerId, int mcsAid)
        {
            if (_bossMemberGroups.TryGetValue(mcsBossPlayerId, out var mcsAids))
            {
                if (mcsAids.Contains(mcsAid))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddGroupMember(MongoId mcsBossPlayerId, int mcsAid)
        {
            var mcsAids = _bossMemberGroups.GetOrAdd(mcsBossPlayerId, _ => new List<int>());
            if (!mcsAids.Contains(mcsAid))
            {
                mcsAids.Add(mcsAid);
            }
        }

        public void RemoveGroupMember(MongoId mcsBossPlayerId, int mcsAid)
        {
            var mcsAids = _bossMemberGroups.GetOrAdd(mcsBossPlayerId, _ => new List<int>());
            if (mcsAids.Contains(mcsAid))
            {
                mcsAids.Remove(mcsAid);
            }
        }

        public void ClearGroupMember(MongoId mcsBossPlayerId)
        {
            _bossMemberGroups.GetOrAdd(mcsBossPlayerId, _ => new List<int>()).Clear();
        }

        public void AcceptGroupInvite(MongoId mcsBossPlayerId, int mcsAid)
        {
            var mcsBotPlayerFullProfile = profileService.GetMcsBotPlayerProfileByAccountId(mcsBossPlayerId, mcsAid);

            if (mcsBotPlayerFullProfile is null)
            {
                return;
            }

            if (CheckMcsPlayerExist(mcsBossPlayerId, mcsAid))
            {
                var notification = notificationHelper.GenerateWsGroupMatchInviteDecline(mcsBotPlayerFullProfile);
                notificationSendHelper.SendMessage(mcsBossPlayerId, notification);
            }
            else
            {
                var notification = notificationHelper.GenerateWsGroupMatchInviteAccept(mcsBotPlayerFullProfile);
                notificationSendHelper.SendMessage(mcsBossPlayerId, notification);

                var notification2 = notificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerFullProfile);
                notificationSendHelper.SendMessage(mcsBossPlayerId, notification2);

                AddGroupMember(mcsBossPlayerId, mcsAid);
            }
        }

        public List<SptProfile> GetAllGroupMemberProfiles(MongoId mcsBossPlayerId)
        {
            var members = _bossMemberGroups.GetOrAdd(mcsBossPlayerId, _ => new List<int>());
            var profiles = new List<SptProfile>();
            foreach (var mcsAid in members)
            {
                var profile = profileService.GetMcsBotPlayerProfileByAccountId(mcsBossPlayerId, mcsAid);
                if (profile is not null)
                {
                    profiles.Add(profile);
                }
            }
            return profiles;
        }
    }
}