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
                await configService.OnPreLoadAsync();
            }
        }

        [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
        public sealed class MiyakoCarryServiceServerPostLoad(
            MCSLocaleService mcsLocaleService, 
            MCSOrderQuestService mcsOrderQuestService,
            MCSTraderService mcsTraderService
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                await mcsLocaleService.OnPostLoadAsync();
                await mcsTraderService.OnPostLoadAsync();
                await mcsOrderQuestService.OnPostLoadAsync();
            }
        }
    }
}