using SPTarkov.DI.Annotations;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSOrderInfoService(
        MCSConfigService mcsConfigService
    )
    {
        private readonly string _orderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "orders");
    }
}