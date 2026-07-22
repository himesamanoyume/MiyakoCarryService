
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

namespace MiyakoCarryService.Server.Services
{

    [Injectable(InjectionType.Singleton)]
    public class LocaleService(
        FileUtil fileUtil,
        JsonUtil jsonUtil,
        ConfigService configService,
        ServerLocalisationService serverLocalisationService,
        DatabaseService databaseService
    )
    {
        private readonly string _globalLocaleFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "locales", "global");
        private readonly string _serverLocaleFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "locales", "server");
        Dictionary<string, Dictionary<string, string>> _globalLocales = [];
        Dictionary<string, Dictionary<string, string>> _serverLocales = [];

        private readonly List<string> _supportedGlobalLocales = ["ch", "ru", "en"];
        private readonly List<string> _supportedServerLocales = ["zh-cn", "zh-TW", "ru", "en"];

        public async Task OnPostLoadAsync()
        {
            _globalLocales = await RecursiveLoadFiles(_globalLocaleFolderDir);
            await FillUnsupportedLocales(_globalLocaleFolderDir, _globalLocales, _supportedGlobalLocales);
            await UpdateGlobalLocales(_globalLocales);
            _serverLocales = await RecursiveLoadFiles(_serverLocaleFolderDir);
            await FillUnsupportedLocales(_serverLocaleFolderDir, _serverLocales, _supportedServerLocales);
            await UpdateServerLocales(_serverLocales);
        }

        public async Task UpdateGlobalLocales(Dictionary<string, Dictionary<string, string>> locales)
        {
            foreach ((var locale, var lazyLoadedValue) in databaseService.GetLocales().Global)
            {
                lazyLoadedValue.AddTransformer(localeData =>
                {
                    if (localeData is null)
                    {
                        return localeData;
                    }

                    locales.TryGetValue(locale, out var globalLocales);
                    if (globalLocales is null)
                    {
                        return localeData;
                    }

                    foreach (var locale in globalLocales)
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

        public async Task UpdateServerLocales(Dictionary<string, Dictionary<string, string>> locales)
        {
            var _loadedLocales = AccessTools.Field(typeof(ServerLocalisationService), "_loadedLocales").GetValue(serverLocalisationService) as Dictionary<string, LazyLoad<Dictionary<string, string>>>;

            foreach (var kvp in locales)
            {
                if (_loadedLocales.TryGetValue(kvp.Key, out var lazyLoadedValue))
                {
                    lazyLoadedValue.AddTransformer(localeData =>
                    {
                        if (localeData is null)
                        {
                            return localeData;
                        }

                        locales.TryGetValue(kvp.Key, out var serverLocales);

                        if (serverLocales is null)
                        {
                            return localeData;
                        }

                        foreach (var locale in serverLocales)
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
        }

        public async Task<Dictionary<string, Dictionary<string, string>>> RecursiveLoadFiles(string path)
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

        public async Task FillUnsupportedLocales(string path, Dictionary<string, Dictionary<string, string>> locales, List<string> supportedLocales)
        {
            if (!locales.TryGetValue("en", out var enLocale) || enLocale is null)
            {
                return;
            }

            foreach (var localeName in locales.Keys.ToList())
            {
                if (supportedLocales.Contains(localeName))
                {
                    continue;
                }

                var copied = new Dictionary<string, string>(enLocale);
                locales[localeName] = copied;

                var filePath = Path.Combine(path, $"{localeName}.json");
                await fileUtil.WriteFileAsync(filePath, jsonUtil.Serialize(copied, true));
            }
        }
    }
}
