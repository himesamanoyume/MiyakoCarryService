
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class MCSConfigService(ModHelper modHelper, JsonUtil jsonUtil)
{
    private readonly string _configFolderPath = Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "Assets", "configs");
    public MCSConfig MCSConfig { get; private set; } = new MCSConfig();
    public MCSOrderConfig MCSOrderConfig { get; private set; }
    private readonly ModMetadata MCSModMetadata = new ModMetadata();
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions() { WriteIndented = true };

    public string GetModPath()
    {
        return modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }

    public SemanticVersioning.Version GetServerVersion()
    {
        return MCSModMetadata.Version;
    }

    public System.Version GetClientVersion()
    {
        return MCSModMetadata.ClientVersion;
    }

    public string GetModUrl()
    {
        return MCSModMetadata.Url;
    }

    public async Task OnPreLoadAsync()
    {
        var miyakoCarryServicePath = Path.Combine(_configFolderPath, "miyakocarryservice.jsonc");
        MCSConfig = await jsonUtil.DeserializeFromFileAsync<MCSConfig>(miyakoCarryServicePath) ?? new MCSConfig();
        var orderConfigPath = Path.Combine(_configFolderPath, "order.json");
        MCSOrderConfig = await jsonUtil.DeserializeFromFileAsync<MCSOrderConfig>(orderConfigPath);
    }

    public MCSConfig GetMiyakoCarryServiceConfig()
    {
        return MCSConfig;
    }

    public MCSOrderConfig GetOrderConfig()
    {
        return MCSOrderConfig;
    }
}