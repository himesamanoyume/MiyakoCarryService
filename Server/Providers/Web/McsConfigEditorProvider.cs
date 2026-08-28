using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Web.Models.Configs;
using SPTarkov.Server.Web.Services;

namespace MiyakoCarryService.Server.Providers.Web;

[Injectable(InjectionType.Singleton)]
public class McsConfigEditorProvider : IConfigEditorConfigProvider
{
    private readonly ConfigService _configService;

    public McsConfigEditorProvider(ConfigService configService)
    {
        _configService = configService;
    }

    public IEnumerable<ConfigEditorConfigRegistration> GetConfigs()
    {
        yield return ConfigEditorConfigRegistration.Create(
            "top.himesamanoyume.miyakocarryservice.mcsconfig",
            "Mcs Config",
            _configService.GetMcsPluginConfig(),
            Path.Combine("user", "mods", "MiyakoCarryServiceServer", "Assets", "configs", "mcsconfig.jsonc")
        );

        yield return new ConfigEditorConfigRegistration
        {
            Id = "top.himesamanoyume.miyakocarryservice.spawntype",
            DisplayName = "Mcs Spawn Types",
            RuntimeConfig = _configService.GetSpawnTypes()
                .OrderBy(kvp => kvp.Key)
                .Where(kvp => kvp.Key != 0)
                .Select(kvp => kvp.Value)
                .ToList(),
            RuntimeType = typeof(List<SpawnType>),
            FilePath = Path.Combine("user", "mods", "MiyakoCarryServiceServer", "Assets", "configs", "spawntype.json"),
            ApplyToRuntimeAsync = (config, _) =>
            {
                _configService.ApplySpawnTypes((List<SpawnType>)config);
                return ValueTask.CompletedTask;
            }
        };
    }
}
