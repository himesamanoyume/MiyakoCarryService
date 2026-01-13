

using System;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;


namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class CompatibilityService(
        // ISptLogger<CompatibilityService> logger
    )
    {
        public bool HasFikaServer { get; private set; } = false;
        public Type FikaMatchService { get; private set; } = null;
        public Type FikaMatch { get; private set; } = null;

        public async Task OnPostLoadAsync()
        {
            CheckFikaServerPlugins();
        }

        private void CheckFikaServerPlugins()
        {
            FikaMatchService = Type.GetType("FikaServer.Services.MatchService, FikaServer");
            HasFikaServer = FikaMatchService is not null;
            FikaMatch = Type.GetType("FikaServer.Models.Fika.FikaMatch, FikaServer");
        }
    }
}