

using System.Collections.Generic;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSTraderService(
        ModHelper modHelper,
        ICloner cloner,
        ImageRouter imageRouter,
        ConfigServer configServer,
        TimeUtil timeUtil,
        DatabaseService databaseService,
        MCSConfigService mcsConfigService
    )
    {
        private readonly string _traderFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "traders", MiyakoTraderId);
        public const string MiyakoTraderId = "6952ced4bcc1dd1e3c80dfcb";

        public async Task OnPostLoadAsync()
        {
            await LoadTrader();
        }

        private Task LoadTrader()
        {
            var iconPath = System.IO.Path.Join(_traderFolderDir, "miyako.jpg");
            var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(_traderFolderDir, "base.json");
            imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), iconPath);
            AddTraderWithEmptyAssortToDb(traderBase);
            var assort = modHelper.GetJsonDataFromFile<TraderAssort>(_traderFolderDir, "assort.json");
            OverwriteTraderAssort(traderBase.Id, assort);
            SetTraderUpdateTime(configServer.GetConfig<TraderConfig>(), traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
            return Task.CompletedTask;
        }

        private void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
        {
            var emptyTraderItemAssortObject = new TraderAssort
            {
                Items = [],
                BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>()
            };

            var traderDataToAdd = new Trader
            {
                Assort = emptyTraderItemAssortObject,
                Base = cloner.Clone(traderDetailsToAdd),
                QuestAssort = new()
                {
                    { "Started", new() },
                    { "Success", new() },
                    { "Fail", new() }
                },
                Dialogue = []
            };

            if (!databaseService.GetTables().Traders.TryAdd(traderDetailsToAdd.Id, traderDataToAdd))
            {
                
            }
        }

        private void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
        {
            if (!databaseService.GetTables().Traders.TryGetValue(traderId, out var traderToEdit))
            {
                return;
            }
            traderToEdit.Assort = newAssorts;
        }

        private void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
        {
            var traderRefreshRecord = new UpdateTime
            {
                TraderId = baseJson.Id,
                Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
            };

            traderConfig.UpdateTime.Add(traderRefreshRecord);
        }
    }
}