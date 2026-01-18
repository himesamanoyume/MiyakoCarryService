
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class ConfigService(
    ConfigServer configServer,
    ModHelper modHelper,
    JsonUtil jsonUtil,
    FileUtil fileUtil
)
{
    private readonly string _configsFolderPath = Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "Assets", "configs");
    public McsConfig McsConfig { get; private set; }
    public OrderConfig OrderConfig { get; private set; }
    private readonly ModMetadata McsModMetadata = new ModMetadata();
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions() { WriteIndented = true };

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

    public async Task OnPreLoadAsync()
    {
        var coreConfig = configServer.GetConfig<CoreConfig>();
        coreConfig.Fixes.RemoveInvalidTradersFromProfile = true;

        var miyakoCarryServicePath = Path.Combine(_configsFolderPath, "mcsconfig.jsonc");
        if (!fileUtil.FileExists(miyakoCarryServicePath))
        {
            await fileUtil.WriteFileAsync(miyakoCarryServicePath, jsonUtil.Serialize(new McsConfig
            {
                ClientConfig = new McsClientConfig()
                {

                },
                ServerConfig = new McsServerConfig()
                {

                }
            }, true));
        }
        McsConfig = await jsonUtil.DeserializeFromFileAsync<McsConfig>(miyakoCarryServicePath);

        var orderConfigPath = Path.Combine(_configsFolderPath, "order.json");
        if (!fileUtil.FileExists(orderConfigPath))
        {
            await fileUtil.WriteFileAsync(orderConfigPath, jsonUtil.Serialize(new OrderConfig
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
                        Reputation = [0.01],
                        RewardSpread = 0.01,
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
                            RequestedItemCount = new MinMax<int> {Min = 300000, Max = 300000},
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
        OrderConfig = await jsonUtil.DeserializeFromFileAsync<OrderConfig>(orderConfigPath);
    }

    public McsConfig GetMiyakoCarryServiceConfig()
    {
        return McsConfig;
    }

    public OrderConfig GetOrderConfig()
    {
        return OrderConfig;
    }
}