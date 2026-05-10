
using System.Collections.Frozen;
using MiyakoCarryService.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
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
        ConfigServer configServer,
        InventoryService inventoryService
    ) : BotInventoryGenerator(
        logger, randomUtil, profileActivityService, botWeaponGenerator, 
        botLootGenerator, botGeneratorHelper, profileHelper, botHelper, 
        weightedRandomHelper, itemHelper, weatherHelper, 
        serverLocalisationService, botEquipmentFilterService, 
        botEquipmentModPoolService, botEquipmentModGenerator, 
        botInventoryContainerService, configServer
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

            // // Get generated raid details bot will be spawned in
            // var raidConfig = profileActivityService.GetProfileActivityRaidData(sessionId)?.RaidConfiguration;

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

            // Roll weapon spawns (primary/secondary/holster) and generate a weapon for each roll that passed
            GenerateAndAddWeaponsToBot(
                botId,
                templateInventory,
                wornItemChances,
                sessionId,
                botInventory,
                botGenerationDetails,
                itemGenerationLimitsMinMax
            );

            // Pick loot and add to bots containers (rig/backpack/pockets/secure)
            mcsBotLootGenerator.CustomGenerateLoot(botId, botJsonTemplate, botGenerationDetails, botInventory);

            // Inventory cache isn't needed, clear to save memory
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
                !BotConfig.Equipment.TryGetValue(
                    botGeneratorHelper.GetBotEquipmentRole(botGenerationDetails.RoleLowercase),
                    out var botEquipConfig
                )
            )
            {
                logger.Error($"Bot Equipment generation failed, unable to find equipment filters for: {botGenerationDetails.RoleLowercase}");

                return;
            }
            var randomistionDetails = botHelper.GetBotRandomizationDetails(100, botEquipConfig);

            // Apply nighttime changes if its nighttime + there's changes to make
            if (randomistionDetails?.NighttimeChanges is not null)
            {
                foreach (var (equipment, weight) in randomistionDetails.NighttimeChanges.EquipmentModsModifiers)
                {
                    if (randomistionDetails.EquipmentMods.TryGetValue(equipment, out var value))
                    {
                        // Never let mod chance go outside 0 - 100
                        // var newWeight = weight + value;
                        randomistionDetails.EquipmentMods[equipment] = 100;
                    }
                }
            }

            // // Is PMC + generating armband + armband forcing is enabled
            // if (PMCConfig.ForceArmband.Enabled && botGenerationDetails.IsPmc)
            // {
            //     // Replace armband pool with single tpl from config
            //     if (templateInventory.Equipment.TryGetValue(EquipmentSlots.ArmBand, out var armbands))
            //     {
            //         // Get tpl based on pmc side
            //         var armbandTpl =
            //             botGenerationDetails.RoleLowercase == "pmcusec" ? PMCConfig.ForceArmband.Usec : PMCConfig.ForceArmband.Bear;

            //         armbands.Clear();
            //         armbands.Add(armbandTpl, 1);

            //         // Force armband spawn to 100%
            //         wornItemChances.EquipmentChances["Armband"] = 100;
            //     }
            // }

            // Get profile of player generating bots, we use their level later on
            // var pmcProfile = profileHelper.GetPmcProfile(sessionId);
            var botEquipmentRole = botGeneratorHelper.GetBotEquipmentRole(botGenerationDetails.RoleLowercase);

            // Iterate over all equipment slots of bot, do it in specific order to reduce conflicts
            // e.g. ArmorVest should be generated after TacticalVest
            // or FACE_COVER before HEADWEAR
            foreach (var (equipmentSlot, itemsWithWeightPool) in templateInventory.Equipment)
            {
                // Skip some slots as they need to be done in a specific order + with specific parameter values
                // e.g. Weapons
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

            // Generate below in specific order
            GenerateEquipment(
                new GenerateEquipmentProperties
                {
                    BotId = botId,
                    RootEquipmentSlot = EquipmentSlots.Pockets,
                    // Unheard profiles have unique sized pockets
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

            // Bot has no armor vest and flagged to be forced to wear armored rig in this event
            if (botEquipConfig.ForceOnlyArmoredRigWhenNoArmor.GetValueOrDefault(false) && !hasArmorVest)
            // Filter rigs down to only those with armor
            {
                FilterRigsToThoseWithProtection(templateInventory.Equipment, botGenerationDetails.RoleLowercase);
            }

            // Optimisation - Remove armored rigs from pool
            if (hasArmorVest)
            // Filter rigs down to only those with armor
            {
                FilterRigsToThoseWithoutProtection(templateInventory.Equipment, botGenerationDetails.RoleLowercase);
            }

            // Bot is flagged as always needing a vest
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
    }
}