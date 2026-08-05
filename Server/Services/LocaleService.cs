
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
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
        LocaleTable localeTable
    )
    {
        private readonly string _globalLocaleFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "locales", "global");
        private readonly string _serverLocaleFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "locales", "server");
        private readonly string _addonLocaleFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "locales", "addon");
        Dictionary<string, Dictionary<string, string>> _globalLocales = [];
        Dictionary<string, Dictionary<string, string>> _serverLocales = [];

        private readonly List<string> _supportedGlobalLocales = ["ch", "ru", "en"];
        private readonly List<string> _supportedServerLocales = ["zh-cn", "zh-TW", "ru", "en"];

        public async Task OnPostLoadAsync()
        {
            _globalLocales = await RecursiveLoadFiles(_globalLocaleFolderDir);
            _serverLocales = await RecursiveLoadFiles(_serverLocaleFolderDir);

            await LoadAddonLocales();

            await FillUnsupportedLocales(_globalLocaleFolderDir, _globalLocales, _supportedGlobalLocales);
            await UpdateGlobalLocales(_globalLocales);
            await FillUnsupportedLocales(_serverLocaleFolderDir, _serverLocales, _supportedServerLocales);
            await UpdateServerLocales(_serverLocales);
        }

        /// <summary>
        /// 加载 addon 目录下所有扩展插件的本地化：遍历 <c>addon/*</c> 下每个插件目录，
        /// 读取其 <c>global</c> / <c>server</c> 子目录中的本地化文件，按 locale 名合并进现有字典（addon 覆盖本体）。
        /// </summary>
        private async Task LoadAddonLocales()
        {
            if (!fileUtil.DirectoryExists(_addonLocaleFolderDir))
            {
                return;
            }

            foreach (var pluginDir in fileUtil.GetDirectories(_addonLocaleFolderDir))
            {
                var globalDir = Path.Join(pluginDir, "global");
                var serverDir = Path.Join(pluginDir, "server");

                if (fileUtil.DirectoryExists(globalDir))
                {
                    MergeLocales(_globalLocales, await RecursiveLoadFiles(globalDir));
                }

                if (fileUtil.DirectoryExists(serverDir))
                {
                    MergeLocales(_serverLocales, await RecursiveLoadFiles(serverDir));
                }
            }
        }

        /// <summary>
        /// 将 <paramref name="source"/> 按 locale 名合并进 <paramref name="target"/>（键级别合并，source 覆盖 target）。
        /// </summary>
        private static void MergeLocales(Dictionary<string, Dictionary<string, string>> target, Dictionary<string, Dictionary<string, string>> source)
        {
            foreach ((var localeName, var localeDict) in source)
            {
                if (target.TryGetValue(localeName, out var existing))
                {
                    foreach ((var key, var value) in localeDict)
                    {
                        existing[key] = value;
                    }
                }
                else
                {
                    target[localeName] = localeDict;
                }
            }
        }

        public async Task UpdateGlobalLocales(Dictionary<string, Dictionary<string, string>> locales)
        {
            foreach ((var locale, var lazyLoadedValue) in localeTable.Global)
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
            var loadedLocales = AccessTools.Property(typeof(ServerLocalisationService), "LoadedLocales").GetValue(serverLocalisationService) as Dictionary<string, LazyLoad<Dictionary<string, string>>>;

            foreach (var kvp in locales)
            {
                if (loadedLocales.TryGetValue(kvp.Key, out var lazyLoadedValue))
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

        /// <summary>
        /// 从已合并的 global 本地化表（含 addon 扩展）中查询指定键的文案，供服务端侧读取客户端文案使用。
        /// 指定语言缺失或键不存在时回退为键名本身。
        /// </summary>
        public string GetGlobalLocalizedText(string key, string locale = "en")
        {
            if (_globalLocales.TryGetValue(locale, out var localeDict) && localeDict.TryGetValue(key, out var value))
            {
                return value;
            }

            return key;
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
