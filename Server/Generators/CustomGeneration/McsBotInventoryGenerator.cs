
using System.Collections.Frozen;
using MiyakoCarryService.Server.Services;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Generators.Loot;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Bot;
using SPTarkov.Server.Core.Helpers.InRaid;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services.Bot;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Services.Profile;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Generators.CustomGeneration
{
    [Injectable]
    public class McsBotInventoryGenerator(
        ISptLogger<BotInventoryGenerator> logger,
        RandomUtil randomUtil,
        ProfileActivityService profileActivityService,
        BotWeaponGenerator botWeaponGenerator,
        BotLootGenerator botLootGenerator,
        McsBotLootGenerator mcsBotLootGenerator,
        BotGeneratorHelper botGeneratorHelper,
        ProfileHelper profileHelper,
        BotHelper botHelper,
        WeightedRandomHelper weightedRandomHelper,
        ItemHelper itemHelper,
        WeatherHelper weatherHelper,
        ServerLocalisationService serverLocalisationService,
        BotEquipmentFilterService botEquipmentFilterService,
        BotEquipmentModPoolService botEquipmentModPoolService,
        BotEquipmentModGenerator botEquipmentModGenerator,
        BotInventoryContainerService botInventoryContainerService,
        BotConfig botConfig, 
        PmcConfig pmcConfig,
        InventoryService inventoryService
    ) : BotInventoryGenerator(logger, randomUtil, profileActivityService, botWeaponGenerator, botLootGenerator, 
        botGeneratorHelper, profileHelper, botHelper, weightedRandomHelper, itemHelper, weatherHelper, 
        serverLocalisationService, botEquipmentFilterService, botEquipmentModPoolService, 
        botEquipmentModGenerator, botInventoryContainerService, botConfig, pmcConfig
    )
    {
        private static readonly FrozenSet<EquipmentSlots> _excludedEquipmentSlots =
        [
            EquipmentSlots.Pockets,
            EquipmentSlots.FirstPrimaryWeapon,
            EquipmentSlots.SecondPrimaryWeapon,
            EquipmentSlots.Holster,
            EquipmentSlots.ArmorVest,
            EquipmentSlots.TacticalVest,
            EquipmentSlots.FaceCover,
            EquipmentSlots.Headwear,
            EquipmentSlots.Earpiece,
            EquipmentSlots.Eyewear
        ];

        public BotBaseInventory CustomGenerateInventory(
            MongoId botId,
            MongoId sessionId,
            BotType botJsonTemplate,
            BotGenerationDetails botGenerationDetails
        )
        {
            var templateInventory = botJsonTemplate.BotInventory;
            var wornItemChances = botJsonTemplate.BotChances;
            var itemGenerationLimitsMinMax = botJsonTemplate.BotGeneration;

            var equipmentId = new MongoId();
            var stashId = new MongoId();
            var questRaidItemsId = new MongoId();
            var questStashItemsId = new MongoId();
            var sortingTableId = new MongoId();
            var hideoutCustomizationStashId = new MongoId();

            var botInventory = new BotBaseInventory
            {
                Items =
                [
                    new Item { Id = equipmentId, Template = ItemTpl.INVENTORY_DEFAULT },
                    new Item { Id = stashId, Template = ItemTpl.STASH_THE_UNHEARD_EDITION_STASH_10X72 },
                    new Item { Id = questRaidItemsId, Template = ItemTpl.STASH_QUESTRAID },
                    new Item { Id = questStashItemsId, Template = ItemTpl.STASH_QUESTOFFLINE },
                    new Item { Id = sortingTableId, Template = ItemTpl.SORTINGTABLE_SORTING_TABLE },
                    new Item { Id = hideoutCustomizationStashId, Template = ItemTpl.HIDEOUTAREACONTAINER_CUSTOMIZATION },
                ],
                Equipment = equipmentId,
                Stash = stashId,
                QuestRaidItems = questRaidItemsId,
                QuestStashItems = questStashItemsId,
                SortingTable = sortingTableId,
                HideoutAreaStashes = new(),
                FastPanel = new(),
                FavoriteItems = [],
                HideoutCustomizationStashId = hideoutCustomizationStashId,
            };

            if (botGenerationDetails.Role is "pmcUSEC" or "pmcBEAR")
            {
                var equipment = inventoryService.GetEquipment();
                if (equipment.Count > 0)
                {
                    templateInventory.Equipment = equipment;
                }

                var ammo = inventoryService.GetAmmo();
                if (ammo.Count > 0)
                {
                    templateInventory.Ammo = ammo;
                }

                var mods = inventoryService.GetMods();
                if (ammo.Count > 0)
                {
                    templateInventory.Mods = mods;
                }
            }

            CustomGenerateAndAddEquipmentToBot(botId, templateInventory, wornItemChances, botInventory, botGenerationDetails);

            GenerateAndAddWeaponsToBot(
                botId,
                templateInventory,
                wornItemChances,
                sessionId,
                botInventory,
                botGenerationDetails,
                itemGenerationLimitsMinMax
            );

            mcsBotLootGenerator.CustomGenerateLoot(botId, botJsonTemplate, botGenerationDetails, botInventory);

            SetMaxDurabilityForItems(botInventory);

            if (botGenerationDetails.ClearBotContainerCacheAfterGeneration)
            {
                botInventoryContainerService.ClearCache(botId);
            }

            return botInventory;
        }

        public void CustomGenerateAndAddEquipmentToBot(
            MongoId botId,
            BotTypeInventory templateInventory,
            Chances wornItemChances,
            BotBaseInventory botInventory,
            BotGenerationDetails botGenerationDetails
        )
        {
            if (
                !botConfig.Equipment.TryGetValue(
                    botGeneratorHelper.GetBotEquipmentRole(botGenerationDetails.RoleLowercase),
                    out var botEquipConfig
                )
            )
            {
                logger.Error($"Bot Equipment generation failed, unable to find equipment filters for: {botGenerationDetails.RoleLowercase}");

                return;
            }
            var randomistionDetails = botHelper.GetBotRandomizationDetails(100, botEquipConfig);

            if (randomistionDetails?.NighttimeChanges is not null)
            {
                foreach (var (equipment, weight) in randomistionDetails.NighttimeChanges.EquipmentModsModifiers)
                {
                    if (randomistionDetails.EquipmentMods.TryGetValue(equipment, out var value))
                    {
                        randomistionDetails.EquipmentMods[equipment] = 100;
                    }
                }
            }

            var botEquipmentRole = botGeneratorHelper.GetBotEquipmentRole(botGenerationDetails.RoleLowercase);

            foreach (var (equipmentSlot, itemsWithWeightPool) in templateInventory.Equipment)
            {
                if (_excludedEquipmentSlots.Contains(equipmentSlot))
                {
                    continue;
                }

                GenerateEquipment(
                    new GenerateEquipmentProperties
                    {
                        BotId = botId,
                        RootEquipmentSlot = equipmentSlot,
                        RootEquipmentPool = itemsWithWeightPool,
                        ModPool = templateInventory.Mods,
                        SpawnChances = wornItemChances,
                        BotData = new BotData
                        {
                            Role = botGenerationDetails.RoleLowercase,
                            Level = 90,
                            EquipmentRole = botEquipmentRole,
                        },
                        Inventory = botInventory,
                        BotEquipmentConfig = botEquipConfig,
                        RandomisationDetails = randomistionDetails,
                        GeneratingPlayerLevel = 90,
                    }
                );
            }

            GenerateEquipment(
                new GenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = EquipmentSlots.Pockets,
                    RootEquipmentPool = GetPocketPoolByGameEdition(
                        botGenerationDetails.GameVersion,
                        templateInventory,
                        botGenerationDetails.IsPmc
                    ),
                    ModPool = templateInventory.Mods,
                    SpawnChances = wornItemChances,
                    BotData = new BotData
                    {
                        Role = botGenerationDetails.RoleLowercase,
                        Level = 90,
                        EquipmentRole = botEquipmentRole,
                    },
                    Inventory = botInventory,
                    BotEquipmentConfig = botEquipConfig,
                    RandomisationDetails = randomistionDetails,
                    GenerateModsBlacklist = [ItemTpl.POCKETS_1X4_TUE, ItemTpl.POCKETS_LARGE],
                    GeneratingPlayerLevel = 90,
                }
            );

            GenerateEquipment(
                new GenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = EquipmentSlots.FaceCover,
                    RootEquipmentPool = templateInventory.Equipment[EquipmentSlots.FaceCover],
                    ModPool = templateInventory.Mods,
                    SpawnChances = wornItemChances,
                    BotData = new BotData
                    {
                        Role = botGenerationDetails.RoleLowercase,
                        Level = 90,
                        EquipmentRole = botEquipmentRole,
                    },
                    Inventory = botInventory,
                    BotEquipmentConfig = botEquipConfig,
                    RandomisationDetails = randomistionDetails,
                    GeneratingPlayerLevel = 90,
                }
            );

            GenerateEquipment(
                new GenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = EquipmentSlots.Headwear,
                    RootEquipmentPool = templateInventory.Equipment[EquipmentSlots.Headwear],
                    ModPool = templateInventory.Mods,
                    SpawnChances = wornItemChances,
                    BotData = new BotData
                    {
                        Role = botGenerationDetails.RoleLowercase,
                        Level = 90,
                        EquipmentRole = botEquipmentRole,
                    },
                    Inventory = botInventory,
                    BotEquipmentConfig = botEquipConfig,
                    RandomisationDetails = randomistionDetails,
                    GeneratingPlayerLevel = 90,
                }
            );

            GenerateEquipment(
                new GenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = EquipmentSlots.Earpiece,
                    RootEquipmentPool = templateInventory.Equipment[EquipmentSlots.Earpiece],
                    ModPool = templateInventory.Mods,
                    SpawnChances = wornItemChances,
                    BotData = new BotData
                    {
                        Role = botGenerationDetails.RoleLowercase,
                        Level = 90,
                        EquipmentRole = botEquipmentRole,
                    },
                    Inventory = botInventory,
                    BotEquipmentConfig = botEquipConfig,
                    RandomisationDetails = randomistionDetails,
                    GeneratingPlayerLevel = 90,
                }
            );

            GenerateEquipment(
                new GenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = EquipmentSlots.Eyewear,
                    RootEquipmentPool = templateInventory.Equipment[EquipmentSlots.Eyewear],
                    ModPool = templateInventory.Mods,
                    SpawnChances = wornItemChances,
                    BotData = new BotData
                    {
                        Role = botGenerationDetails.RoleLowercase,
                        Level = 90,
                        EquipmentRole = botEquipmentRole,
                    },
                    Inventory = botInventory,
                    BotEquipmentConfig = botEquipConfig,
                    RandomisationDetails = randomistionDetails,
                    GeneratingPlayerLevel = 90,
                }
            );

            var hasArmorVest = GenerateEquipment(
                new GenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = EquipmentSlots.ArmorVest,
                    RootEquipmentPool = templateInventory.Equipment[EquipmentSlots.ArmorVest],
                    ModPool = templateInventory.Mods,
                    SpawnChances = wornItemChances,
                    BotData = new BotData
                    {
                        Role = botGenerationDetails.RoleLowercase,
                        Level = 90,
                        EquipmentRole = botEquipmentRole,
                    },
                    Inventory = botInventory,
                    BotEquipmentConfig = botEquipConfig,
                    RandomisationDetails = randomistionDetails,
                    GeneratingPlayerLevel = 90,
                }
            );

            if (botEquipConfig.ForceOnlyArmoredRigWhenNoArmor.GetValueOrDefault(false) && !hasArmorVest)
            {
                FilterRigsToThoseWithProtection(templateInventory.Equipment, botGenerationDetails.RoleLowercase);
            }

            if (hasArmorVest)
            {
                FilterRigsToThoseWithoutProtection(templateInventory.Equipment, botGenerationDetails.RoleLowercase);
            }

            if (botEquipConfig.ForceRigWhenNoVest.GetValueOrDefault(false) && !hasArmorVest)
            {
                wornItemChances.EquipmentChances["TacticalVest"] = 100;
            }

            GenerateEquipment(
                new GenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = EquipmentSlots.TacticalVest,
                    RootEquipmentPool = templateInventory.Equipment[EquipmentSlots.TacticalVest],
                    ModPool = templateInventory.Mods,
                    SpawnChances = wornItemChances,
                    BotData = new BotData
                    {
                        Role = botGenerationDetails.RoleLowercase,
                        Level = 90,
                        EquipmentRole = botEquipmentRole,
                    },
                    Inventory = botInventory,
                    BotEquipmentConfig = botEquipConfig,
                    RandomisationDetails = randomistionDetails,
                    GeneratingPlayerLevel = 90,
                }
            );
        }

        public void SetMaxDurabilityForItems(BotBaseInventory botInventory)
        {
            foreach (var item in botInventory.Items)
            {
                if (item.Upd?.Repairable != null)
                {
                    item.Upd.Repairable.Durability = item.Upd.Repairable.MaxDurability;
                }
            }
        }
    }
}