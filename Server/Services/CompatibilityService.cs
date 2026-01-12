

using System;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;


namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class CompatibilityService(
        // ISptLogger<CompatibilityService> logger
    )
    {
        private bool _hasFikaServer = false;

        public async Task OnPostLoadAsync()
        {
            CheckFikaServerPlugins();
        }

        public bool HasFikaServer()
        {
            return _hasFikaServer;
        }

        private void CheckFikaServerPlugins()
        {
            _hasFikaServer = Type.GetType("FikaServer.Services.MatchService, FikaServer") is not null;
        }
    }
}