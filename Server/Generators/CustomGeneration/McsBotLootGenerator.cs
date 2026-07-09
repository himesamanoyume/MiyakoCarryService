
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Generators.Loot;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Bot;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services.Bot;
using SPTarkov.Server.Core.Services.Locales;
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
        BotConfig botConfig, 
        PmcConfig pmcConfig,
        ICloner cloner
    ) : BotLootGenerator(
        logger, randomUtil, itemHelper, inventoryHelper, handbookHelper,
        botGeneratorHelper, botWeaponGenerator, weightedRandomHelper,
        botHelper, botLootCacheService, serverLocalisationService, botConfig, pmcConfig, cloner
    )
    {
        public void CustomGenerateLoot(
            MongoId botId,
            BotType botJsonTemplate,
            BotGenerationDetails botGenerationDetails,
            BotBaseInventory botInventory
        )
        {
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

            var healingItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Healing.Weights);
            var drugItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Drugs.Weights);
            var foodItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Food.Weights);
            var drinkItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Drink.Weights);
            var stimItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Stims.Weights);

            if (botGenerationDetails.IsPmc && pmcConfig.ForceHealingItemsIntoSecure)
            {
                AddForcedMedicalItemsToPmcSecure(botInventory, botGenerationDetails.RoleLowercase, botId);
            }

            var botItemLimits = GetItemSpawnLimitsForBot(botGenerationDetails.RoleLowercase);
            var containersBotHasAvailable = GetAvailableContainersBotCanStoreItemsIn(botInventory);

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

            // Secure
            if (!botGenerationDetails.IsPmc || (botGenerationDetails.IsPmc && pmcConfig.AddSecureContainerLootFromBotConfig))
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