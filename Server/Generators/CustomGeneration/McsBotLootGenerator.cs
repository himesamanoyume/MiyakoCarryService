
using System.Linq;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
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
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Generators.CustomGeneration
{
    [Injectable]
    public class McsBotLootGenerator(
        ISptLogger<BotLootGenerator> logger,
        RandomUtil randomUtil,
        ItemHelper itemHelper,
        InventoryHelper inventoryHelper,
        HandbookHelper handbookHelper,
        BotGeneratorHelper botGeneratorHelper,
        BotWeaponGenerator botWeaponGenerator,
        WeightedRandomHelper weightedRandomHelper,
        BotHelper botHelper,
        BotLootCacheService botLootCacheService,
        ServerLocalisationService serverLocalisationService,
        ConfigServer configServer,
        ICloner cloner
    ) : BotLootGenerator(
        logger, randomUtil, itemHelper, inventoryHelper, handbookHelper,
        botGeneratorHelper, botWeaponGenerator, weightedRandomHelper,
        botHelper, botLootCacheService, serverLocalisationService, configServer, cloner
    )
    {
        public void CustomGenerateLoot(
            MongoId botId,
            // MongoId sessionId,
            BotType botJsonTemplate,
            BotGenerationDetails botGenerationDetails,
            BotBaseInventory botInventory
        )
        {
            // Limits on item types to be added as loot
            var itemCounts = botJsonTemplate.BotGeneration?.Items;

            if (
                itemCounts?.BackpackLoot.Weights is null
                || itemCounts.PocketLoot.Weights is null
                || itemCounts.VestLoot.Weights is null
                || itemCounts.SpecialItems.Weights is null
                || itemCounts.Healing.Weights is null
                || itemCounts.Drugs.Weights is null
                || itemCounts.Food.Weights is null
                || itemCounts.Drink.Weights is null
                || itemCounts.Currency.Weights is null
                || itemCounts.Stims.Weights is null
                || itemCounts.Grenades.Weights is null
            )
            {
                logger.Warning(serverLocalisationService.GetText("bot-unable_to_generate_bot_loot", botGenerationDetails.RoleLowercase));
                return;
            }

            // var backpackLootCount = weightedRandomHelper.GetWeightedValue(itemCounts.BackpackLoot.Weights);
            // var pocketLootCount = weightedRandomHelper.GetWeightedValue(itemCounts.PocketLoot.Weights);
            // var vestLootCount = weightedRandomHelper.GetWeightedValue(itemCounts.VestLoot.Weights);
            // var specialLootItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.SpecialItems.Weights);
            var healingItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Healing.Weights);
            var drugItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Drugs.Weights);
            var foodItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Food.Weights);
            var drinkItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Drink.Weights);
            // var currencyItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Currency.Weights);
            var stimItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Stims.Weights);
            var grenadeCount = weightedRandomHelper.GetWeightedValue(itemCounts.Grenades.Weights);

            // // If bot has been flagged as not having loot, set below counts to 0
            // if (BotConfig.DisableLootOnBotTypes.Contains(botGenerationDetails.RoleLowercase))
            // {
            //     backpackLootCount = 0;
            //     pocketLootCount = 0;
            //     vestLootCount = 0;
            //     currencyItemCount = 0;
            // }

            // Forced pmc healing loot into secure container
            if (botGenerationDetails.IsPmc && PMCConfig.ForceHealingItemsIntoSecure)
            {
                AddForcedMedicalItemsToPmcSecure(botInventory, botGenerationDetails.RoleLowercase, botId);
            }

            var botItemLimits = GetItemSpawnLimitsForBot(botGenerationDetails.RoleLowercase);

            var containersBotHasAvailable = GetAvailableContainersBotCanStoreItemsIn(botInventory);

            // // Special items
            // AddLootFromPool(
            //     botId,
            //     botLootCacheService.GetLootFromCache(
            //         botGenerationDetails.RoleLowercase,
            //         botGenerationDetails.IsPmc,
            //         LootCacheType.Special,
            //         botJsonTemplate
            //     ),
            //     containersBotHasAvailable,
            //     specialLootItemCount,
            //     botInventory,
            //     botGenerationDetails.RoleLowercase,
            //     botItemLimits
            // );

            // Healing items / Meds
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.HealingItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                healingItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // Drugs
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.DrugItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                drugItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // Food
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.FoodItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                foodItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // Drink
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.DrinkItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                drinkItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // // Currency
            // AddLootFromPool(
            //     botId,
            //     botLootCacheService.GetLootFromCache(
            //         botGenerationDetails.RoleLowercase,
            //         botGenerationDetails.IsPmc,
            //         LootCacheType.CurrencyItems,
            //         botJsonTemplate
            //     ),
            //     containersBotHasAvailable,
            //     currencyItemCount,
            //     botInventory,
            //     botGenerationDetails.RoleLowercase,
            //     null,
            //     0,
            //     botGenerationDetails.IsPmc
            // );

            // Stims
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.StimItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                stimItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                botItemLimits,
                0,
                botGenerationDetails.IsPmc
            );

            // Grenades
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.GrenadeItems,
                    botJsonTemplate
                ),
                [EquipmentSlots.Pockets, EquipmentSlots.TacticalVest], // Can't use containersBotHasEquipped as we don't want grenades added to backpack
                grenadeCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // var itemPriceLimits = GetSingleItemLootPriceLimits(botGenerationDetails.BotLevel, botGenerationDetails.IsPmc);

            // // Backpack - generate loot if they have one
            // if (containersBotHasAvailable.Contains(EquipmentSlots.Backpack) && backpackLootCount > 0)
            // {
            //     // Add randomly generated weapon to PMC backpacks
            //     if (botGenerationDetails.IsPmc && randomUtil.GetChance100(PMCConfig.LooseWeaponInBackpackChancePercent))
            //     {
            //         AddLooseWeaponsToInventorySlot(
            //             botId,
            //             sessionId,
            //             botInventory,
            //             EquipmentSlots.Backpack,
            //             botGenerationDetails,
            //             botJsonTemplate.BotInventory,
            //             botJsonTemplate.BotChances?.WeaponModsChances
            //         );
            //     }

            //     var backpackLootRoubleTotal = botGenerationDetails.IsPmc
            //         ? PMCConfig.LootSettings.Backpack.GetRoubleValue(botGenerationDetails.BotLevel, botGenerationDetails.Location)
            //         : 0;

            //     AddLootFromPool(
            //         botId,
            //         botLootCacheService.GetLootFromCache(
            //             botGenerationDetails.RoleLowercase,
            //             botGenerationDetails.IsPmc,
            //             LootCacheType.Backpack,
            //             botJsonTemplate,
            //             itemPriceLimits?.Backpack
            //         ),
            //         [EquipmentSlots.Backpack],
            //         backpackLootCount,
            //         botInventory,
            //         botGenerationDetails.RoleLowercase,
            //         botItemLimits,
            //         backpackLootRoubleTotal,
            //         botGenerationDetails.IsPmc
            //     );
            // }

            // var vestLootRoubleTotal = botGenerationDetails.IsPmc
            //     ? PMCConfig.LootSettings.Vest.GetRoubleValue(botGenerationDetails.BotLevel, botGenerationDetails.Location)
            //     : 0;

            // // TacticalVest - generate loot if they have one
            // if (containersBotHasAvailable.Contains(EquipmentSlots.TacticalVest))
            // // Vest
            // {
            //     AddLootFromPool(
            //         botId,
            //         botLootCacheService.GetLootFromCache(
            //             botGenerationDetails.RoleLowercase,
            //             botGenerationDetails.IsPmc,
            //             LootCacheType.Vest,
            //             botJsonTemplate,
            //             itemPriceLimits?.Vest
            //         ),
            //         [EquipmentSlots.TacticalVest],
            //         vestLootCount,
            //         botInventory,
            //         botGenerationDetails.RoleLowercase,
            //         botItemLimits,
            //         vestLootRoubleTotal,
            //         botGenerationDetails.IsPmc
            //     );
            // }

            // var pocketLootRoubleTotal = botGenerationDetails.IsPmc
            //     ? PMCConfig.LootSettings.Pocket.GetRoubleValue(botGenerationDetails.BotLevel, botGenerationDetails.Location)
            //     : 0;

            // // Pockets
            // AddLootFromPool(
            //     botId,
            //     botLootCacheService.GetLootFromCache(
            //         botGenerationDetails.RoleLowercase,
            //         botGenerationDetails.IsPmc,
            //         LootCacheType.Pocket,
            //         botJsonTemplate,
            //         itemPriceLimits?.Pocket
            //     ),
            //     [EquipmentSlots.Pockets],
            //     pocketLootCount,
            //     botInventory,
            //     botGenerationDetails.RoleLowercase,
            //     botItemLimits,
            //     pocketLootRoubleTotal,
            //     botGenerationDetails.IsPmc
            // );

            // Secure

            // only add if not a pmc or is pmc and flag is true
            if (!botGenerationDetails.IsPmc || (botGenerationDetails.IsPmc && PMCConfig.AddSecureContainerLootFromBotConfig))
            {
                AddLootFromPool(
                    botId,
                    botLootCacheService.GetLootFromCache(
                        botGenerationDetails.RoleLowercase,
                        botGenerationDetails.IsPmc,
                        LootCacheType.Secure,
                        botJsonTemplate
                    ),
                    [EquipmentSlots.SecuredContainer],
                    50,
                    botInventory,
                    botGenerationDetails.RoleLowercase,
                    null,
                    -1,
                    botGenerationDetails.IsPmc
                );
            }
        }
    }
}