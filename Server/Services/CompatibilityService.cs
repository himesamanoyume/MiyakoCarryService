

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Mod;


namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class CompatibilityService(
        IReadOnlyList<SptMod> loadedMods
        // SptLogger<CompatibilityService> logger
    )
    {
        public bool HasFikaServer { get; private set; } = false;
        public bool HasAPBS { get; private set; } = false;
        public Type FikaMatchServiceType { get; private set; } = null;
        public Type FikaMatchType { get; private set; } = null;

        public async Task OnPostLoadAsync()
        {
            HasFikaServer = loadedMods.Any(mod => mod.ModMetadata.ModGuid == "Fika");
            if (HasFikaServer)
            {
                CheckFikaServerType();
            }
            HasAPBS = loadedMods.Any(mod => mod.ModMetadata.ModGuid == "com.acidphantasm.progressivebotsystem");
        }

        private void CheckFikaServerType()
        {
            FikaMatchServiceType = Type.GetType("FikaServer.Services.MatchService, FikaServer");
            FikaMatchType = Type.GetType("FikaServer.Models.Fika.FikaMatch, FikaServer");
        }
    }
}