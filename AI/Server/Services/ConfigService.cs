
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MiyakoCarryService.AI.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.AI.Server.Services;

[Injectable(InjectionType.Singleton)]
public class ConfigService(
    ModHelper modHelper,
    JsonUtil jsonUtil,
    FileUtil fileUtil
)
{
    private readonly string _configsFolderPath = Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "Assets", "configs");
    private McsAIConfig _mcsAIConfig;
    private readonly ModMetadata McsModMetadata = new();
    public static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public string GetModPath()
    {
        return modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }

    public SemanticVersioning.Version GetServerVersion()
    {
        return McsModMetadata.Version;
    }

    public System.Version GetClientVersion()
    {
        return McsModMetadata.ClientVersion;
    }

    public string GetModUrl()
    {
        return McsModMetadata.Url;
    }

    private async Task LoadMcsAIConfig()
    {
        var mcsPluginConfigPath = Path.Combine(_configsFolderPath, "mcsaiconfig.jsonc");
        if (!fileUtil.FileExists(mcsPluginConfigPath))
        {
            await fileUtil.WriteFileAsync(mcsPluginConfigPath, jsonUtil.Serialize(new McsAIConfig(), true));
        }
        _mcsAIConfig = await jsonUtil.DeserializeFromFileAsync<McsAIConfig>(mcsPluginConfigPath);
        await SaveMcsPluginConfig();
    }

    private async Task SaveMcsPluginConfig()
    {
        var mcsPluginConfigPath = Path.Combine(_configsFolderPath, "mcsaiconfig.jsonc");
        var jsonMcsPluginConfig = jsonUtil.Serialize(_mcsAIConfig, true);
        await fileUtil.WriteFileAsync(mcsPluginConfigPath, jsonMcsPluginConfig);
    }

    public async Task OnPreLoadAsync()
    {
        await LoadMcsAIConfig();
    }

    public McsAIConfig GetMcsAIConfig()
    {
        return _mcsAIConfig;
    }
}