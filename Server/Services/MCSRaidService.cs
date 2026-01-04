using System;
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
    public sealed class MCSRaidService(
        MCSNotificationHelper mcsNotificationHelper,
        NotificationSendHelper notificationSendHelper,
        MCSProfileService mcsProfileService
    )
    {
        private readonly ConcurrentDictionary<MongoId, List<int>> _bossGroup = new();

        public async Task OnPostLoadAsync()
        {

        }

        public bool CheckCSPlayerExist(MongoId bossSession, int csAid)
        {
            if (_bossGroup.TryGetValue(bossSession, out var csAids))
            {
                if (csAids.Contains(csAid))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddGroupMember(MongoId bossSession, int csAid)
        {
            _bossGroup.GetOrAdd(bossSession, _ => new List<int>()).Add(csAid);
        }

        public void RemoveGroupMember(MongoId bossSession, int csAid)
        {
            _bossGroup.GetOrAdd(bossSession, _ => new List<int>()).Remove(csAid);
        }

        public void AcceptGroupInvite(MongoId bossSessionId, int csAid)
        {
            var csFullProfile = mcsProfileService.GetCSFullProfileByAccountId(bossSessionId, csAid);

            if (csFullProfile is null)
            {
                return;
            }

            if (CheckCSPlayerExist(bossSessionId, csAid))
            {
                var notification = mcsNotificationHelper.GenerateWsGroupMatchInviteDecline(csFullProfile);
                notificationSendHelper.SendMessage(bossSessionId, notification);
            }
            else
            {
                var notification = mcsNotificationHelper.GenerateWsGroupMatchInviteAccept(csFullProfile);
                notificationSendHelper.SendMessage(bossSessionId, notification);

                var notification2 = mcsNotificationHelper.GenerateWsGroupMatchRaidReady(csFullProfile);
                notificationSendHelper.SendMessage(bossSessionId, notification2);
            }
        }
    }
}