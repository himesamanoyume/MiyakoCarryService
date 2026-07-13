using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Services;

namespace MiyakoCarryService.Server
{
    public class MiyakoCarryServiceServer
    {
        [Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
        public class MiyakoCarryServiceServerPreLoad(
            ConfigService configService
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                await configService.OnPreLoadAsync();
            }
        }

        [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
        public class MiyakoCarryServiceServerPostLoad(
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                
            }
        }
    }
}