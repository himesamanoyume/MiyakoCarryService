
using System;
using System.Linq;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
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
        TimeUtil timeUtil,
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
        public BotBase CustomPrepareAndGenerateBot(MongoId sessionId, BotGenerationDetails botGenerationDetails, OrderInfo orderInfo)
        {
            var botBaseClone = GetPreparedBotBaseClone(
                botGenerationDetails.EventRole ?? botGenerationDetails.Role,
                botGenerationDetails.Side,
                botGenerationDetails.BotDifficulty
            );

            var botRole = botGenerationDetails.IsPmc
                ? botBaseClone.Info.Side
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

            return CustomGenerateBot(sessionId, botBaseClone, botJsonTemplateClone, botGenerationDetails, orderInfo);
        }

        protected BotBase CustomGenerateBot(MongoId sessionId, BotBase bot, BotType botJsonTemplate, BotGenerationDetails botGenerationDetails, OrderInfo orderInfo)
        {
            botGenerationDetails.RoleLowercase = botGenerationDetails.Role.ToLowerInvariant();

            AddIdsToBot(bot);

            if (!botGenerationDetails.IsPlayerScav)
            {
                botEquipmentFilterService.FilterBotEquipment(sessionId, botJsonTemplate, botGenerationDetails);
            }

            bot.Info.Nickname = botNameService.GenerateUniqueBotNickname(
                botJsonTemplate,
                botGenerationDetails,
                BotConfig.BotRolesThatMustHaveUniqueName
            );

            bot.Info.LowerNickname = botGenerationDetails.IsPmc ? bot.Info.Nickname.ToLowerInvariant() : string.Empty;

            if (!botGenerationDetails.IsPlayerScav && ShouldSimulatePlayerScav(botGenerationDetails.RoleLowercase))
            {
                botNameService.AddRandomPmcNameToBotMainProfileNicknameProperty(bot);
                SetRandomisedGameVersionAndCategory(bot.Info);
            }

            if (!seasonalEventService.ChristmasEventEnabled())
            {
                if (botGenerationDetails.Role != "gifter")
                {
                    seasonalEventService.RemoveChristmasItemsFromBotInventory(botJsonTemplate.BotInventory, botGenerationDetails.Role);
                }
            }

            RemoveBlacklistedLootFromBotTemplate(botJsonTemplate.BotInventory);

            if (!(botGenerationDetails.IsPmc || botGenerationDetails.IsPlayerScav))
            {
                bot.Hideout = null;
            }

            var expTable = databaseService.GetGlobals().Configuration.Exp.Level.ExperienceTable;
            bot.Info.Experience = expTable.Take(botGenerationDetails.PlayerLevel.Value).Sum(entry => entry.Experience);
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

            bot.Skills = new Skills
            {
                Common = Enum.GetValues<SkillTypes>()
                    .Select(skill => new CommonSkill
                    {
                        Id = skill,
                        Progress = GetRandomProgressByCarryServiceLevel(orderInfo.CarryServiceLevel),
                        PointsEarnedDuringSession = 0,
                        LastAccess = timeUtil.GetTimeStamp(),
                    }).ToList(),
                Mastering = GetMasteringSkillsWithRandomisedProgressValue(botJsonTemplate.BotSkills.Mastering),
                Points = 0,
            };

            bot.Info.PrestigeLevel = 0;

            if (botGenerationDetails.IsPmc)
            {
                bot.Info.IsStreamerModeAvailable = true;
                SetRandomisedGameVersionAndCategory(bot.Info);
                if (bot.Info.GameVersion == GameEditions.UNHEARD)
                {
                    AddAdditionalPocketLootWeightsForUnheardBot(botJsonTemplate);
                }

                botGenerationDetails.GameVersion = bot.Info.GameVersion;
            }

            SetBotAppearance(bot, botJsonTemplate.BotAppearance, botGenerationDetails);

            bot.Inventory = mcsBotInventoryGenerator.CustomGenerateInventory(bot.Id.Value, sessionId, botJsonTemplate, botGenerationDetails);

            if (BotConfig.BotRolesWithDogTags.Contains(botGenerationDetails.RoleLowercase))
            {
                AddDogtagToBot(bot);
            }

            GenerateInventoryId(bot);

            if (botGenerationDetails.EventRole is not null)
            {
                bot.Info.Settings.Role = botGenerationDetails.EventRole;
            }

            return bot;
        }

        private double GetRandomProgressByCarryServiceLevel(int carryServiceLevel)
        {
            return carryServiceLevel switch
            {
                1 => randomUtil.RandInt(0, 1000),
                2 => randomUtil.RandInt(1000, 2000),
                3 => randomUtil.RandInt(2000, 3000),
                4 => randomUtil.RandInt(3000, 4000),
                >=5 => randomUtil.RandInt(4000, 5100),
                _ => randomUtil.RandInt(0, 1000)
            };
        }
    }
}