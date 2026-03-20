
using System.Linq;
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
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Generators.CustomGeneration
{
    [Injectable]
    public class McsBotGenerator(
        ISptLogger<BotGenerator> logger,
        HashUtil hashUtil,
        RandomUtil randomUtil,
        DatabaseService databaseService,
        BotInventoryGenerator botInventoryGenerator,
        McsBotInventoryGenerator mcsBotInventoryGenerator,
        BotLevelGenerator botLevelGenerator,
        BotEquipmentFilterService botEquipmentFilterService,
        WeightedRandomHelper weightedRandomHelper,
        BotHelper botHelper,
        SeasonalEventService seasonalEventService,
        ItemFilterService itemFilterService,
        BotNameService botNameService,
        ConfigServer configServer,
        ICloner cloner
    ) : BotGenerator(
        logger, hashUtil, randomUtil, databaseService, botInventoryGenerator, 
        botLevelGenerator, botEquipmentFilterService, weightedRandomHelper, 
        botHelper, seasonalEventService, itemFilterService, botNameService, 
        configServer, cloner
    )
    {
        public BotBase CustomPrepareAndGenerateBot(MongoId sessionId, BotGenerationDetails botGenerationDetails)
        {
            var botBaseClone = GetPreparedBotBaseClone(
                botGenerationDetails.EventRole ?? botGenerationDetails.Role, // Use eventRole if provided
                botGenerationDetails.Side,
                botGenerationDetails.BotDifficulty
            );

            // Get raw json data for bot (Cloned)
            var botRole = botGenerationDetails.IsPmc
                ? botBaseClone.Info.Side // Use side to get usec.json or bear.json when bot will be PMC
                : botGenerationDetails.Role;
            var botJsonTemplateClone = cloner.Clone(botHelper.GetBotTemplate(botRole));
            if (botJsonTemplateClone is null)
            {
                logger.Error($"Unable to retrieve: {botRole} bot template, cannot generate bot of this type");
            }

            // 所有装备部位100%生成
            foreach (var slot in botJsonTemplateClone.BotChances.EquipmentChances.Keys.ToList())  
            {  
                if (slot is "Armband")
                {
                    botJsonTemplateClone.BotChances.EquipmentChances[slot] = 0;
                    continue;
                }

                botJsonTemplateClone.BotChances.EquipmentChances[slot] = 100;  
            }  
            
            // 强制所有模组100%生成
            foreach (var slot in botJsonTemplateClone.BotChances.EquipmentModsChances.Keys.ToList())  
            {  
                botJsonTemplateClone.BotChances.EquipmentModsChances[slot] = 100;  
            }

            foreach (var slot in botJsonTemplateClone.BotChances.WeaponModsChances.Keys.ToList())  
            {  
                botJsonTemplateClone.BotChances.WeaponModsChances[slot] = 100;  
            }

            return CustomGenerateBot(sessionId, botBaseClone, botJsonTemplateClone, botGenerationDetails);
        }

        protected BotBase CustomGenerateBot(MongoId sessionId, BotBase bot, BotType botJsonTemplate, BotGenerationDetails botGenerationDetails)
        {
            botGenerationDetails.RoleLowercase = botGenerationDetails.Role.ToLowerInvariant();

            // Generate Id/AId for bot
            AddIdsToBot(bot);

            // Only filter bot equipment, never players
            if (!botGenerationDetails.IsPlayerScav)
            {
                botEquipmentFilterService.FilterBotEquipment(sessionId, botJsonTemplate, botGenerationDetails);
            }

            bot.Info.Nickname = botNameService.GenerateUniqueBotNickname(
                botJsonTemplate,
                botGenerationDetails,
                BotConfig.BotRolesThatMustHaveUniqueName
            );

            // Only PMCs need a lower nickname
            bot.Info.LowerNickname = botGenerationDetails.IsPmc ? bot.Info.Nickname.ToLowerInvariant() : string.Empty;

            // Only run when generating a 'fake' playerscav, not actual player scav
            if (!botGenerationDetails.IsPlayerScav && ShouldSimulatePlayerScav(botGenerationDetails.RoleLowercase))
            {
                botNameService.AddRandomPmcNameToBotMainProfileNicknameProperty(bot);
                SetRandomisedGameVersionAndCategory(bot.Info);
            }

            if (!seasonalEventService.ChristmasEventEnabled())
            // Process all bots EXCEPT gifter, he needs christmas items
            {
                if (botGenerationDetails.Role != "gifter")
                {
                    seasonalEventService.RemoveChristmasItemsFromBotInventory(botJsonTemplate.BotInventory, botGenerationDetails.Role);
                }
            }

            RemoveBlacklistedLootFromBotTemplate(botJsonTemplate.BotInventory);

            // Remove hideout data if bot is not a PMC or pscav - match what live sends
            if (!(botGenerationDetails.IsPmc || botGenerationDetails.IsPlayerScav))
            {
                bot.Hideout = null;
            }

            bot.Info.Experience = 0;
            bot.Info.Level = botGenerationDetails.PlayerLevel;
            bot.Info.Settings.Experience = GetExperienceRewardForKillByDifficulty(
                botJsonTemplate.BotExperience.Reward,
                botGenerationDetails.BotDifficulty,
                botGenerationDetails.Role
            );
            bot.Info.Settings.StandingForKill = GetStandingChangeForKillByDifficulty(
                botJsonTemplate.BotExperience.StandingForKill,
                botGenerationDetails.BotDifficulty,
                botGenerationDetails.Role
            );
            bot.Info.Settings.AggressorBonus = GetAggressorBonusByDifficulty(
                botJsonTemplate.BotExperience.StandingForKill,
                botGenerationDetails.BotDifficulty,
                botGenerationDetails.Role
            );
            bot.Info.Settings.UseSimpleAnimator = botJsonTemplate.BotExperience.UseSimpleAnimator;
            bot.Customization.Voice = weightedRandomHelper.GetWeightedValue(botJsonTemplate.BotAppearance.Voice);
            bot.Health = GenerateHealth(botJsonTemplate.BotHealth, botGenerationDetails.IsPlayerScav);

            // 需要定制（需要先知道技能是否会对AI的行为产生影响）
            bot.Skills = GenerateSkills(botJsonTemplate.BotSkills);
            // end
            bot.Info.PrestigeLevel = 0;

            if (botGenerationDetails.IsPmc)
            {
                bot.Info.IsStreamerModeAvailable = true; // Set to true so client patches can pick it up later - client sometimes alters botrole to assaultGroup
                SetRandomisedGameVersionAndCategory(bot.Info);
                if (bot.Info.GameVersion == GameEditions.UNHEARD)
                {
                    AddAdditionalPocketLootWeightsForUnheardBot(botJsonTemplate);
                }

                botGenerationDetails.GameVersion = bot.Info.GameVersion;
            }

            // Add drip
            SetBotAppearance(bot, botJsonTemplate.BotAppearance, botGenerationDetails);

            bot.Inventory = mcsBotInventoryGenerator.CustomGenerateInventory(bot.Id.Value, sessionId, botJsonTemplate, botGenerationDetails);

            if (BotConfig.BotRolesWithDogTags.Contains(botGenerationDetails.RoleLowercase))
            {
                AddDogtagToBot(bot);
            }

            // Generate new inventory ID
            GenerateInventoryId(bot);

            // Set role back to originally requested now it has been generated
            if (botGenerationDetails.EventRole is not null)
            {
                bot.Info.Settings.Role = botGenerationDetails.EventRole;
            }

            return bot;
        }
    }
}