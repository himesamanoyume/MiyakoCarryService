using SPTarkov.DI.Annotations;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSProfileService(
        MCSConfigService mcsConfigService
    )
    {
        private readonly string _profileFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "profiles");
    }
}