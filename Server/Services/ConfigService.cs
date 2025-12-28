
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class ConfigService(ModHelper modHelper, JsonUtil jsonUtil)
{
    private readonly string configFolderPath = Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "Assets/config");
    public MiyakoCarryServiceConfig Config { get; private set; } = new MiyakoCarryServiceConfig();
    private readonly ModMetadata MiyakoCarryServiceServerModMetadata = new ModMetadata();
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions() { WriteIndented = true };

    public string GetModPath()
    {
        return modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }

    public SemanticVersioning.Version GetServerVersion()
    {
        return MiyakoCarryServiceServerModMetadata.Version;
    }

    public System.Version GetClientVersion()
    {
        return MiyakoCarryServiceServerModMetadata.ClientVersion;
    }

    public string GetModUrl()
    {
        return MiyakoCarryServiceServerModMetadata.Url;
    }

    public async Task OnPreLoad()
    {
        string configPath = Path.Combine(configFolderPath, "miyakocarryservice.jsonc");
        Config = await jsonUtil.DeserializeFromFileAsync<MiyakoCarryServiceConfig>(configPath) ?? new MiyakoCarryServiceConfig();
    }

    public MiyakoCarryServiceConfig GetConfig()
    {
        return Config;
    }

}

public record MiyakoCarryServiceClientConfig
{

}

public record MiyakoCarryServiceServerConfig
{
    
}

public sealed class MiyakoCarryServiceConfig
{
    [JsonPropertyName("Client")]
    public MiyakoCarryServiceClientConfig ClientConfig { get; set; } = new MiyakoCarryServiceClientConfig();

    [JsonPropertyName("Server")]
    public MiyakoCarryServiceServerConfig ServerConfig { get; set; } = new MiyakoCarryServiceServerConfig();
}