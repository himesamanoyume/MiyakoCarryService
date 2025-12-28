using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Services;

namespace MiyakoCarryService.Server
{
    public sealed class MiyakoCarryServiceServer
    {
        [Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader)]
        public sealed class MiyakoCarryServiceServerPreLoad(ConfigService configService) : IOnLoad
        {
            public async Task OnLoad()
            {
                await configService.OnPreLoad();
            }

        }

        [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
        public sealed class MiyakoCarryServiceServerPostLoad(LocaleService localeService, MiyakoTraderService miyakoTraderService) : IOnLoad
        {
            public async Task OnLoad()
            {
                await localeService.OnPostLoadAsync();
                await miyakoTraderService.OnPostLoadAsync();
            }
        }
    }
}