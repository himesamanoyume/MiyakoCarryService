using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Services;
using MiyakoCarryService.Server.Patches;

namespace MiyakoCarryService.Server
{
    public sealed class MiyakoCarryServiceServer
    {
        [Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
        public sealed class MiyakoCarryServiceServerPreLoad(
            MCSConfigService configService
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                new GetClientRepeatableQuestsPatch().Enable();
                new ChangeRepeatableQuestPatch().Enable();
                new CompleteQuestPatch().Enable();
                new GetOtherProfilePatch().Enable();
                new GetFriendListPatch().Enable();
                await configService.OnPreLoadAsync();
            }
        }

        [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
        public sealed class MiyakoCarryServiceServerPostLoad(
            MCSLocaleService mcsLocaleService, 
            MCSOrderQuestService mcsOrderQuestService,
            MCSTraderService mcsTraderService,
            MCSProfileService mcsProfileService,
            MCSOrderInfoService mcsOrderInfoService
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                await mcsLocaleService.OnPostLoadAsync();
                await mcsTraderService.OnPostLoadAsync();
                await mcsProfileService.OnPostLoadAsync();
                await mcsOrderQuestService.OnPostLoadAsync();
                await mcsOrderInfoService.OnPostLoadAsync();
            }
        }
    }
}