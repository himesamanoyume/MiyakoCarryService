
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services
{

    [Injectable(InjectionType.Singleton)]
    public sealed class InventoryService(
        ConfigService configService,
        JsonUtil jsonUtil,
        FileUtil fileUtil
    )
    {
        private readonly string _inventoryFolderDir = Path.Join(configService.GetModPath(), "Assets", "database", "bots", "inventory");
        Dictionary<EquipmentSlots, Dictionary<MongoId, double>> _equipment = [];
        Dictionary<string, Dictionary<MongoId, double>> _ammo = [];
        Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> _mods = [];

        public async Task OnPostLoadAsync()
        {
            await LoadEquipment();
            await LoadAmmo();
            await LoadMods();
        }

        public Dictionary<EquipmentSlots, Dictionary<MongoId, double>> GetEquipment()
        {
            return _equipment;
        }

        public Dictionary<string, Dictionary<MongoId, double>> GetAmmo()
        {
            return _ammo;
        }

        public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> GetMods()
        {
            return _mods;
        }

        private async Task LoadEquipment()
        {
            var equipmentPath = System.IO.Path.Combine(_inventoryFolderDir, "equipment.json");
            if (!fileUtil.FileExists(equipmentPath))
            {
                await fileUtil.WriteFileAsync(equipmentPath, "{}");
            }

            _equipment = await jsonUtil.DeserializeFromFileAsync<Dictionary<EquipmentSlots, Dictionary<MongoId, double>>>(equipmentPath);
        }

        private async Task LoadAmmo()
        {
            var ammoPath = System.IO.Path.Combine(_inventoryFolderDir, "ammo.json");
            if (!fileUtil.FileExists(ammoPath))
            {
                await fileUtil.WriteFileAsync(ammoPath, "{}");
            }

            _ammo = await jsonUtil.DeserializeFromFileAsync<Dictionary<string, Dictionary<MongoId, double>>>(ammoPath);
        }

        private async Task LoadMods()
        {
            var modsPath = System.IO.Path.Combine(_inventoryFolderDir, "mods.json");
            if (!fileUtil.FileExists(modsPath))
            {
                await fileUtil.WriteFileAsync(modsPath, "{}");
            }

            _mods = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>(modsPath);
        }
    }
}
