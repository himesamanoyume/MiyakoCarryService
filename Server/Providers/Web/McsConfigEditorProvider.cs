using System.Collections.Generic;
using System.IO;
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
            filePath: Path.Combine("user", "mods", "MiyakoCarryServiceServer", "Assets", "configs", "mcsconfig.jsonc")
        );

        var spawnTypes = _configService.GetSpawnTypes().Values;
        yield return ConfigEditorConfigRegistration.Create(
            "top.himesamanoyume.miyakocarryservice.spawntype",
            "Spawn Types",
            spawnTypes,
            Path.Combine("user", "mods", "MiyakoCarryServiceServer", "Assets", "configs", "spawntype.json")
        );
    }
}
