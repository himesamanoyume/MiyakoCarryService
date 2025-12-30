using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSOrderQuestService(
        ModHelper modHelper,
        MCSConfigService mcsConfigService
    )
    {
        private readonly string _traderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "templates");
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
    }

}