using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Helper;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;

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

        public bool CheckCSPlayerExist(MongoId bossSessionId, int csAid)
        {
            if (_bossMemberGroups.TryGetValue(bossSessionId, out var csAids))
            {
                if (csAids.Contains(csAid))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddGroupMember(MongoId bossSessionId, int csAid)
        {
            var csAids = _bossMemberGroups.GetOrAdd(bossSessionId, _ => new List<int>());
            if (!csAids.Contains(csAid))
            {
                csAids.Add(csAid);
            }
        }

        public void RemoveGroupMember(MongoId bossSessionId, int csAid)
        {
            var csAids = _bossMemberGroups.GetOrAdd(bossSessionId, _ => new List<int>());
            if (csAids.Contains(csAid))
            {
                csAids.Remove(csAid);
            }
        }

        public void ClearGroupMember(MongoId bossSessionId)
        {
            _bossMemberGroups.GetOrAdd(bossSessionId, _ => new List<int>()).Clear();
        }

        public void AcceptGroupInvite(MongoId bossSessionId, int csAid)
        {
            var csFullProfile = profileService.GetCSFullProfileByAccountId(bossSessionId, csAid);

            if (csFullProfile is null)
            {
                return;
            }

            if (CheckCSPlayerExist(bossSessionId, csAid))
            {
                var notification = notificationHelper.GenerateWsGroupMatchInviteDecline(csFullProfile);
                notificationSendHelper.SendMessage(bossSessionId, notification);
            }
            else
            {
                var notification = notificationHelper.GenerateWsGroupMatchInviteAccept(csFullProfile);
                notificationSendHelper.SendMessage(bossSessionId, notification);

                var notification2 = notificationHelper.GenerateWsGroupMatchRaidReady(csFullProfile);
                notificationSendHelper.SendMessage(bossSessionId, notification2);

                AddGroupMember(bossSessionId, csAid);
            }
        }
    }
}