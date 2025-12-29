
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class ConfigService(ModHelper modHelper, JsonUtil jsonUtil)
{
    private readonly string configFolderPath = Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "Assets/config");
    public MiyakoCarryServiceConfig MiyakoCarryServiceConfig { get; private set; } = new MiyakoCarryServiceConfig();
    public OrderConfig OrderConfig { get; private set; }
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
        var miyakoCarryServicePath = Path.Combine(configFolderPath, "miyakocarryservice.jsonc");
        MiyakoCarryServiceConfig = await jsonUtil.DeserializeFromFileAsync<MiyakoCarryServiceConfig>(miyakoCarryServicePath) ?? new MiyakoCarryServiceConfig();
        var orderConfigPath = Path.Combine(configFolderPath, "order.json");
        OrderConfig = await jsonUtil.DeserializeFromFileAsync<OrderConfig>(orderConfigPath);
    }

    public MiyakoCarryServiceConfig GetMiyakoCarryServiceConfig()
    {
        return MiyakoCarryServiceConfig;
    }

    public OrderConfig GetOrderConfig()
    {
        return OrderConfig;
    }
}

public record MiyakoCarryServiceClientConfig
{

}

public record MiyakoCarryServiceServerConfig
{
    
}

public record MiyakoCarryServiceConfig
{
    [JsonPropertyName("Client")]
    public MiyakoCarryServiceClientConfig ClientConfig { get; set; }

    [JsonPropertyName("Server")]
    public MiyakoCarryServiceServerConfig ServerConfig { get; set; }
}

public record OrderConfig
{
    [JsonPropertyName("orderQuests")]
    public required List<RepeatableQuestConfig> OrderQuests { get; set; }
}
