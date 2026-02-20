
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using MiyakoCarryService.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class ConfigService(
    ModHelper modHelper,
    ISptLogger<ConfigService> logger,
    JsonUtil jsonUtil,
    FileUtil fileUtil
)
{
    private readonly string _configsFolderPath = Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "Assets", "configs");
    private McsPluginConfig _mcsConfig;
    private OrderConfig _orderConfig;
    private ConcurrentDictionary<int, BotType> _botTypes = new();
    private readonly ModMetadata McsModMetadata = new();
    public static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public string GetModPath()
    {
        return modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }

    public SemanticVersioning.Version GetServerVersion()
    {
        return McsModMetadata.Version;
    }

    public System.Version GetClientVersion()
    {
        return McsModMetadata.ClientVersion;
    }

    public string GetModUrl()
    {
        return McsModMetadata.Url;
    }

    private async Task LoadMcsConfig()
    {
        var mcsConfigPath = Path.Combine(_configsFolderPath, "mcsconfig.jsonc");
        if (!fileUtil.FileExists(mcsConfigPath))
        {
            await fileUtil.WriteFileAsync(mcsConfigPath, jsonUtil.Serialize(new McsPluginConfig
            {
                ClientConfig = new McsPluginClientConfig()
                {

                },
                ServerConfig = new McsPluginServerConfig()
                {

                }
            }, true));
        }
        _mcsConfig = await jsonUtil.DeserializeFromFileAsync<McsPluginConfig>(mcsConfigPath);
    }

    private async Task LoadOrderConfig()
    {
        var orderPath = Path.Combine(_configsFolderPath, "order.json");
        if (!fileUtil.FileExists(orderPath))
        {
            await fileUtil.WriteFileAsync(orderPath, jsonUtil.Serialize(new OrderConfig
            {
                OrderQuests = [new RepeatableQuestConfig
                {
                    Id = "6953358977d6d54857304681",
                    Name = "Order",
                    Side = PlayerGroup.Pmc,
                    Types = ["Completion"],
                    ResetTime = 900,
                    NumQuests = 100,
                    MinPlayerLevel = 0,
                    RewardScaling = new RewardScaling
                    {
                        Levels = [0],
                        Experience = [0],
                        Roubles = [0],
                        GpCoins = [0],
                        Items = [0],
                        Reputation = [0],
                        RewardSpread = 0,
                        SkillRewardChance = [0],
                        SkillPointReward = [0]
                    },
                    Locations = new() {
                        {ELocationName.any, ["any"]}
                    },
                    TraderWhitelist = [],
                    QuestConfig = new RepeatableQuestTypesConfig
                    {
                        ExplorationConfig = [new ExplorationConfig
                        {
                            LevelRange = new MinMax<int> {Min = 0, Max = 0},
                            MinimumExtracts = 0,
                            MaximumExtracts = 0,
                            MinimumExtractsWithSpecificExit = 0,
                            MaximumExtractsWithSpecificExit = 0,
                            PossibleSkillRewards = [],
                            SpecificExits = new SpecificExits
                            {
                                Chance = 0,
                                PassageRequirementWhitelist = []
                            }
                        }],
                        CompletionConfig = [new CompletionConfig
                        {
                            LevelRange = new MinMax<int> {Min = 0, Max = 9999},
                            PossibleSkillRewards = [],
                            RequestedItemCount = new MinMax<int> {Min = 100000, Max = 100000},
                            UniqueItemCount = new MinMax<int> {Min = 0, Max = 0},
                            RequestedBulletCount = new MinMax<int> {Min = 0, Max = 0},
                            UseWhitelist = false,
                            UseBlacklist = false,
                            RequiredItemsAreFiR = false,
                            RequiredItemMinDurabilityMinMax = new MinMax<int> {Min = 0, Max = 0},
                            RequiredItemTypeBlacklist = []
                        }],
                        Elimination = [new EliminationConfig
                        {
                            LevelRange = new MinMax<int> {Min = 0, Max = 0},
                            PossibleSkillRewards = [],
                            Targets = [],
                            BodyPartChance = 0,
                            BodyParts = [],
                            SpecificLocationChance = 0,
                            DistLocationBlacklist = [],
                            DistanceProbability = 0,
                            MaxDistance = 0,
                            MinDistance = 0,
                            MaxKills = 0,
                            MinKills = 0,
                            MaxBossKills = 0,
                            MinBossKills = 0,
                            MaxPmcKills = 0,
                            MinPmcKills = 0,
                            WeaponRequirementChance = 0,
                            WeaponCategoryRequirementChance = 0,
                            WeaponCategoryRequirements = [],
                            WeaponRequirements = []
                        }]
                    },
                    RewardBaseTypeBlacklist = [],
                    RewardBlacklist = [],
                    RewardAmmoStackMinSize = 0,
                    FreeChangesAvailable = 0,
                    FreeChanges = 0,
                    KeepDailyQuestTypeOnReplacement = false,
                    StandingChangeCost = [0]
                }]
            }, true));
        }
        _orderConfig = await jsonUtil.DeserializeFromFileAsync<OrderConfig>(orderPath);
    }

    private async Task LoadBotTypeConfig()
    {
        var botTypeConfigPath = Path.Combine(_configsFolderPath, "bottype.json");
        if (!fileUtil.FileExists(botTypeConfigPath))
        {
            var botTypeList = new List<BotType>
            {
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossBoar.ToString(),
                    IsBoss = true,
                    DisplayName = "Kaban"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossBully.ToString(),
                    IsBoss = true,
                    DisplayName = "Reshala"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossGluhar.ToString(),
                    IsBoss = true,
                    DisplayName = "Gluhar"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossKilla.ToString(),
                    IsBoss = true,
                    DisplayName = "Killa"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossKnight.ToString(),
                    IsBoss = true,
                    DisplayName = "Knight"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.followerBigPipe.ToString(),
                    IsBoss = true,
                    DisplayName = "BigPipe"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.followerBirdEye.ToString(),
                    IsBoss = true,
                    DisplayName = "BirdEye"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossKolontay.ToString(),
                    IsBoss = true,
                    DisplayName = "Kolontay"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossKojaniy.ToString(),
                    IsBoss = true,
                    DisplayName = "Shturman"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossSanitar.ToString(),
                    IsBoss = true,
                    DisplayName = "Sanitar"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossTagilla.ToString(),
                    IsBoss = true,
                    DisplayName = "Tagilla"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossPartisan.ToString(),
                    IsBoss = true,
                    DisplayName = "Partisan"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossZryachiy.ToString(),
                    IsBoss = true,
                    DisplayName = "Zryachiy"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossTagillaAgro.ToString(),
                    IsBoss = true,
                    DisplayName = "Tagilla Agro"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.bossKillaAgro.ToString(),
                    IsBoss = true,
                    DisplayName = "Killa Agro"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.infectedTagilla.ToString(),
                    IsBoss = true,
                    DisplayName = "Infected Tagilla"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.exUsec.ToString(),
                    IsBoss = false,
                    DisplayName = "Rouge"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.infectedPmc.ToString(),
                    IsBoss = false,
                    DisplayName = "Infected Pmc"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.infectedAssault.ToString(),
                    IsBoss = false,
                    DisplayName = "Infected Scav"
                },
                new BotType
                {
                    WildSpawnType = WildSpawnType.pmcBot.ToString(),
                    IsBoss = false,
                    DisplayName = "Raider"
                },
            };
            await fileUtil.WriteFileAsync(botTypeConfigPath, jsonUtil.Serialize(botTypeList, true));
        }

        var botTypes = await jsonUtil.DeserializeFromFileAsync<List<BotType>>(botTypeConfigPath);
        botTypes.Insert(0, GenerateCommonBotType(false));

        for (int i = 0; i < botTypes.Count; i++)
        {
            _botTypes.TryAdd(i, botTypes[i]);
        }
    }

    public async Task OnPreLoadAsync()
    {
        // var coreConfig = configServer.GetConfig<CoreConfig>();
        // coreConfig.Fixes.RemoveInvalidTradersFromProfile = true;
        await LoadMcsConfig();
        await LoadOrderConfig();
        await LoadBotTypeConfig();
    }

    public McsPluginConfig GetMiyakoCarryServiceConfig()
    {
        return _mcsConfig;
    }

    public OrderConfig GetOrderConfig()
    {
        return _orderConfig;
    }

    public ConcurrentDictionary<int, BotType> GetBotTypeConfig()
    {
        return _botTypes;
    }

    public BotType TryGetBotType(int index)
    {
        return _botTypes.GetOrAdd(index, _ => GenerateCommonBotType(true));
    }

    private BotType GenerateCommonBotType(bool log)
    {
        if (log)
        {
            logger.Warning($"你正在尝试获取一个不存在的BotType, 将返回默认类型");
        }

        return new BotType
        {
            WildSpawnType = "common",
            IsBoss = false,
            DisplayName = Locales.BOTTYPECOMMON
        };
    }
}