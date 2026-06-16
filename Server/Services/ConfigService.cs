
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
    private ConcurrentDictionary<int, SpawnType> _spawnTypes = new();
    private readonly ModMetadata McsModMetadata = new();
    public static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private System.Version _latestVersion = new("1.0.5.0");
    public bool HaveUpdate = false;

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

    public void UpdateLatestVersion(System.Version version)
    {
        _latestVersion = version;
    }

    public System.Version GetLatestVersion()
    {
        return _latestVersion;
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
                    MinPlayerLevel = 1,
                    RewardScaling = new RewardScaling
                    {
                        Levels = [1],
                        Experience = [1],
                        Roubles = [1],
                        GpCoins = [1],
                        Items = [1],
                        Reputation = [1],
                        RewardSpread = 0,
                        SkillRewardChance = [1],
                        SkillPointReward = [1]
                    },
                    Locations = new() {
                        {ELocationName.any, ["any"]}
                    },
                    TraderWhitelist = [],
                    QuestConfig = new RepeatableQuestTypesConfig
                    {
                        ExplorationConfig = [new ExplorationConfig
                        {
                            LevelRange = new MinMax<int> {Min = 1, Max = 1},
                            MinimumExtracts = 1,
                            MaximumExtracts = 1,
                            MinimumExtractsWithSpecificExit = 1,
                            MaximumExtractsWithSpecificExit = 1,
                            PossibleSkillRewards = [],
                            SpecificExits = new SpecificExits
                            {
                                Chance = 1,
                                PassageRequirementWhitelist = []
                            }
                        }],
                        CompletionConfig = [new CompletionConfig
                        {
                            LevelRange = new MinMax<int> {Min = 1, Max = 9999},
                            PossibleSkillRewards = [],
                            RequestedItemCount = new MinMax<int> {Min = 30000, Max = 30000},
                            UniqueItemCount = new MinMax<int> {Min = 1, Max = 1},
                            RequestedBulletCount = new MinMax<int> {Min = 1, Max = 1},
                            UseWhitelist = false,
                            UseBlacklist = false,
                            RequiredItemsAreFiR = false,
                            RequiredItemMinDurabilityMinMax = new MinMax<int> {Min = 1, Max = 1},
                            RequiredItemTypeBlacklist = []
                        }],
                        Elimination = [new EliminationConfig
                        {
                            LevelRange = new MinMax<int> {Min = 1, Max = 1},
                            PossibleSkillRewards = [],
                            Targets = [],
                            BodyPartChance = 1,
                            BodyParts = [],
                            SpecificLocationChance = 1,
                            DistLocationBlacklist = [],
                            DistanceProbability = 1,
                            MaxDistance = 1,
                            MinDistance = 1,
                            MaxKills = 1,
                            MinKills = 1,
                            MaxBossKills = 1,
                            MinBossKills = 1,
                            MaxPmcKills = 1,
                            MinPmcKills = 1,
                            WeaponRequirementChance = 1,
                            WeaponCategoryRequirementChance = 1,
                            WeaponCategoryRequirements = [],
                            WeaponRequirements = []
                        }]
                    },
                    RewardBaseTypeBlacklist = [],
                    RewardBlacklist = [],
                    RewardAmmoStackMinSize = 1,
                    FreeChangesAvailable = 0,
                    FreeChanges = 0,
                    KeepDailyQuestTypeOnReplacement = false,
                    StandingChangeCost = [0]
                }]
            }, true));
        }
        _orderConfig = await jsonUtil.DeserializeFromFileAsync<OrderConfig>(orderPath);
    }

    public string GetSpawnTypeDisplayName(string wildSpawnType)
    {
        foreach (var kvp in _spawnTypes)
        {
            if (kvp.Value.WildSpawnType == wildSpawnType)
            {
                return kvp.Value.DisplayName;
            }
        }

        return wildSpawnType switch
        {
            "pmcBEAR" => "Bear",
            "pmcUSEC" => "Usec",
            "assault" => "Scav",
            _ => wildSpawnType
        };
    }

    private async Task LoadSpawnTypeConfig()
    {
        var spawnTypeConfigPath = Path.Combine(_configsFolderPath, "spawntype.json");
        if (!fileUtil.FileExists(spawnTypeConfigPath))
        {
            var spawnTypeList = new List<SpawnType>
            {
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossBoar.ToString(),
                    IsBoss = true,
                    DisplayName = "Kaban"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossBully.ToString(),
                    IsBoss = true,
                    DisplayName = "Reshala"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossGluhar.ToString(),
                    IsBoss = true,
                    DisplayName = "Gluhar"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossKilla.ToString(),
                    IsBoss = true,
                    DisplayName = "Killa"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossKnight.ToString(),
                    IsBoss = true,
                    DisplayName = "Knight"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.followerBigPipe.ToString(),
                    IsBoss = true,
                    DisplayName = "BigPipe"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.followerBirdEye.ToString(),
                    IsBoss = true,
                    DisplayName = "BirdEye"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossKolontay.ToString(),
                    IsBoss = true,
                    DisplayName = "Kolontay"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossKojaniy.ToString(),
                    IsBoss = true,
                    DisplayName = "Shturman"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossSanitar.ToString(),
                    IsBoss = true,
                    DisplayName = "Sanitar"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossTagilla.ToString(),
                    IsBoss = true,
                    DisplayName = "Tagilla"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossPartisan.ToString(),
                    IsBoss = true,
                    DisplayName = "Partisan"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossZryachiy.ToString(),
                    IsBoss = true,
                    DisplayName = "Zryachiy"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossTagillaAgro.ToString(),
                    IsBoss = true,
                    DisplayName = "Tagilla Agro"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.bossKillaAgro.ToString(),
                    IsBoss = true,
                    DisplayName = "Killa Agro"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.infectedTagilla.ToString(),
                    IsBoss = true,
                    DisplayName = "Infected Tagilla"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.exUsec.ToString(),
                    IsBoss = false,
                    DisplayName = "Rouge"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.infectedPmc.ToString(),
                    IsBoss = false,
                    DisplayName = "Infected Pmc"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.infectedAssault.ToString(),
                    IsBoss = false,
                    DisplayName = "Infected Scav"
                },
                new SpawnType
                {
                    WildSpawnType = WildSpawnType.pmcBot.ToString(),
                    IsBoss = false,
                    DisplayName = "Raider"
                },
            };
            await fileUtil.WriteFileAsync(spawnTypeConfigPath, jsonUtil.Serialize(spawnTypeList, true));
        }

        var spawnTypes = await jsonUtil.DeserializeFromFileAsync<List<SpawnType>>(spawnTypeConfigPath);
        spawnTypes.Insert(0, GenerateCommonSpawnType(false));

        for (int i = 0; i < spawnTypes.Count; i++)
        {
            _spawnTypes.TryAdd(i, spawnTypes[i]);
        }
    }

    public async Task OnPreLoadAsync()
    {
        await LoadMcsConfig();
        await LoadOrderConfig();
        await LoadSpawnTypeConfig();
    }

    public McsPluginConfig GetMiyakoCarryServiceConfig()
    {
        return _mcsConfig;
    }

    public OrderConfig GetOrderConfig()
    {
        return _orderConfig;
    }

    public ConcurrentDictionary<int, SpawnType> GetSpawnTypes()
    {
        return _spawnTypes;
    }

    public SpawnType TryGetSpawnType(int index)
    {
        return _spawnTypes.GetOrAdd(index, _ => GenerateCommonSpawnType(true));
    }

    private SpawnType GenerateCommonSpawnType(bool log)
    {
        if (log)
        {
            logger.Warning($"你正在尝试获取一个不存在的SpawnType, 将返回默认类型");
        }

        return new SpawnType
        {
            WildSpawnType = "common",
            IsBoss = false,
            DisplayName = Locales.SPAWNTYPECOMMON
        };
    }

    public List<string> GetAllCustomBrainName()
    {
        var customNames = new List<string>();
        foreach (var kvp in _spawnTypes)
        {
            var spawnType = kvp.Value;
            if (!string.IsNullOrEmpty(spawnType.BrainName))
            {
                customNames.Add(spawnType.BrainName);
            }
        }
        return customNames;
    }
}