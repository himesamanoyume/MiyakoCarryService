

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
        public Type FikaMatchServiceType { get; private set; } = null;
        public Type FikaMatchType { get; private set; } = null;

        public async Task OnPostLoadAsync()
        {
            CheckFikaServerPlugins();
        }

        private void CheckFikaServerPlugins()
        {
            FikaMatchServiceType = Type.GetType("FikaServer.Services.MatchService, FikaServer");
            HasFikaServer = FikaMatchServiceType is not null;
            FikaMatchType = Type.GetType("FikaServer.Models.Fika.FikaMatch, FikaServer");
        }
    }
}