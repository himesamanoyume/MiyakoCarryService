
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services
{

    [Injectable(InjectionType.Singleton)]
    public sealed class LocaleService(
        FileUtil fileUtil,
        ConfigService configService,
        DatabaseService databaseService
    )
    {
        private readonly string _globalLocaleFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "locales", "global");
        private readonly string _serverLocaleFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "locales", "server");
        Dictionary<string, Dictionary<string, string>> _globalLocales = [];
        Dictionary<string, Dictionary<string, string>> _serverLocales = [];

        public async Task OnPostLoadAsync()
        {
            _globalLocales = await RecursiveLoadFiles(_globalLocaleFolderDir);
            _serverLocales = await RecursiveLoadFiles(_serverLocaleFolderDir);
            await UpdateGlobalLocales(_globalLocales);
            await UpdateGlobalLocales(_serverLocales);
        }

        private async Task UpdateGlobalLocales(Dictionary<string, Dictionary<string, string>> locales)
        {
            foreach ((var locale, var lazyLoadedValue) in databaseService.GetLocales().Global)
            {
                lazyLoadedValue.AddTransformer(localeData =>
                {
                    if (localeData is null)
                    {
                        return localeData;
                    }

                    locales.TryGetValue(locale, out var miyakoCarryServiceLocales);
                    if (miyakoCarryServiceLocales is null)
                    {
                        return localeData;
                    }

                    foreach (var locale in miyakoCarryServiceLocales)
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
}
