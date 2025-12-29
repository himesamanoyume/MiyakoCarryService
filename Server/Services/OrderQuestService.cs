using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.Mod;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class OrderQuestService(
        ModHelper modHelper,
        CustomQuestService customQuestService,
        ConfigService MiyakoCarryServiceConfig)
    {
        private readonly string _traderDir = System.IO.Path.Join(MiyakoCarryServiceConfig.GetModPath(), "Assets", "database", "templates");
        private RepeatableQuest _orderTemplate;
        public async Task OnPostLoadAsync()
        {
            LoadOrderTemplate();
        }

        private void LoadOrderTemplate()
        {
            var orderTemplate = modHelper.GetJsonDataFromFile<RepeatableQuest>(_traderDir, "orderQuests.json");
            _orderTemplate = orderTemplate;
        }

        public RepeatableQuest GetOrderTemplate()
        {
            return _orderTemplate;
        }

        private void CreateQuest(RepeatableQuest repeatableQuest)
        {
            var newQuestDetails = new NewQuestDetails
            {
                NewQuest = repeatableQuest,
                Locales = null
            };
            customQuestService.CreateQuest(newQuestDetails);
        }
    }

}