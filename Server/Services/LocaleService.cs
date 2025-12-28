
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class LocaleService(
    FileUtil fileUtil, 
    ConfigService MiyakoCarryServiceConfig, 
    DatabaseService databaseService)
{
    private readonly string globalLocaleDir = Path.Join(MiyakoCarryServiceConfig.GetModPath(), "Assets", "database", "locales", "global");
    Dictionary<string, Dictionary<string, string>> _globalLocales = [];

    public async Task OnPostLoadAsync()
    {
        await LoadGlobalLocales();
        LoadServerLocales();
    }

    private async Task LoadGlobalLocales()
    {
        _globalLocales = await RecursiveLoadFiles(globalLocaleDir);

        foreach ((var locale, var lazyLoadedValue) in databaseService.GetLocales().Global)
        {
            lazyLoadedValue.AddTransformer(localeData =>
            {
                if (localeData is null)
                {
                    return localeData;
                }

                _globalLocales.TryGetValue(locale, out var MiyakoCarryServiceLocales);
                if (MiyakoCarryServiceLocales is null)
                {
                    return localeData;
                }
                
                foreach (var locale in MiyakoCarryServiceLocales)
                {
                    if (localeData.ContainsKey(locale.Key))
                    {
                        localeData[locale.Key] = locale.Value;
                    }
                    else
                    {
                        localeData.Add(locale.Key, locale.Value);
                    }
                }

                return localeData;
            });
        }
    }

    private void LoadServerLocales()
    {

    }

    private async Task<Dictionary<string, Dictionary<string, string>>> RecursiveLoadFiles(string path)
    {
        List<string> files = fileUtil.GetFiles(path);

        Dictionary<string, Dictionary<string, string>> locales = [];

        foreach (string file in files)
        {
            await using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                var localeFile = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(fs);

                locales.Add(Path.GetFileNameWithoutExtension(file), localeFile);
            }
        }

        return locales;
    }
}