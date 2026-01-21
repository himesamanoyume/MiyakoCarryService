
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 从服务端获取所有要生成的护航Bot数据
    /// </summary>
    internal sealed class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        private static SquadMgr _squadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsPresets), nameof(BotsPresets.TryLoadBotsProfilesOnStart));

        [PatchPostfix]
        public static async void Postfix(Task __result)
        {
            await __result;

            var mcsProfilesDict = await McsRequestHandler.GetCarryServicePlayer();

            foreach (var mcsProfileItem in mcsProfilesDict)
            {
                foreach (var mcsProfile in mcsProfileItem.Value)
                {
                    await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid, PoolManagerClass.AssemblyType.Online, mcsProfile.GetAllPrefabPaths(false).ToArray(), JobPriorityClass.General, new Progress<LoadingProgressStruct>(), default);
                }
            }

            var gameWorld = Singleton<GameWorld>.Instance;

            var bossPlayers = mcsProfilesDict
                .Where(kvp => kvp.Value.Length > 0)
                .Select(kvp => gameWorld.GetEverExistedPlayerByID(kvp.Key))
                .Where(bossPlayer => bossPlayer != null);

            var botGame = Singleton<IBotGame>.Instance;
            var botsController = botGame.BotsController;
            var botSpawner = botsController.BotSpawner;
            var botCreator = botSpawner.BotCreator;

            MiyakoCarryServicePlugin.Logger.LogInfo("bossPlayer Count: "+ bossPlayers.Count());

            foreach (var bossPlayer in bossPlayers)
            {
                var bossPlayerPos = bossPlayer.Position;
                var wildSpawnType = bossPlayer.Side switch
                {
                    EPlayerSide.Bear => WildSpawnType.pmcBEAR,
                    EPlayerSide.Usec => WildSpawnType.pmcUSEC,
                    _ => WildSpawnType.assault
                };

                var mcsAIBossPlayer = new McsAIBossPlayer(bossPlayer);

                bossPlayer.Profile.Info.GroupId = bossPlayer.Profile.Info.GroupId == "fika" ? "fika" : "mcs";

                var botSpawnParams = new BotSpawnParams();
                botSpawnParams.ShallBeGroup = new ShallBeGroupParams(true, false, 5);

                var botProfileDataClass = new BotProfileDataClass(bossPlayer.Side, wildSpawnType, BotDifficulty.hard, 2, botSpawnParams);

                var botCreationDataClass = await BotCreationDataClass.Create(botProfileDataClass, botCreator, 0, botSpawner);

                botCreationDataClass.AddProfiles(mcsProfilesDict.SelectMany(item => item.Value).ToList());

                var closestGroupPoint = botsController.CoversData.GetClosest(bossPlayerPos);
                botCreationDataClass.AddPosition(bossPlayerPos, closestGroupPoint.CorePointInGame.Id);

                var closestZone = botSpawner.GetClosestZone(bossPlayerPos, out _);

                var groupAction = new Func<BotOwner, BotZone, BotsGroup>((BotOwner botOwner, BotZone botZone) =>
                {
                    // if (myPlayer.Side != EPlayerSide.Savage)
                    // {

                    _squadMgr.AddMcsSquadMember(bossPlayer.ProfileId, botOwner.ProfileId, botOwner);
                    if (bossPlayer.BotsGroup != null)
                    {
                        // var otherCsPlayers = csProfilesDict[bossPlayer.ProfileId].Select(otherCsPlayerProfile => gameWorld.GetEverExistedPlayerByID(otherCsPlayerProfile.ProfileId));

                        // foreach (var otherCsPlayer in otherCsPlayers)
                        // {
                        //     if (otherCsPlayer != null)
                        //     {
                        //         bossPlayer.BotsGroup.RemoveEnemy(otherCsPlayer);
                        //         bossPlayer.BotsGroup.AddAlly(otherCsPlayer);
                        //     }
                        // }
                        return bossPlayer.BotsGroup;
                    }

                    botOwner.Settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = bossPlayer.Side == EPlayerSide.Savage;
                    botOwner.Settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = bossPlayer.Side != EPlayerSide.Savage;

                    var oldReasons = botOwner.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY;

                    botOwner.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = true;
                    botOwner.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = [];

                    botOwner.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;

                    botOwner.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
                    botOwner.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;

                    var enemyTypes = botOwner.Settings.GetEnemyBotTypes();
                    
                    if (!enemyTypes.Contains(WildSpawnType.pmcBEAR))
                    {
                        enemyTypes.Add(WildSpawnType.pmcBEAR);
                    }
                    if (!enemyTypes.Contains(WildSpawnType.pmcUSEC))
                    {
                        enemyTypes.Add(WildSpawnType.pmcUSEC);
                    }

                    var enemies = botSpawner.method_5(botOwner);

                    var botsGroup = new BotsGroup(closestZone, botGame, botOwner, enemies.ToList(), botSpawner.DeadBodiesController, botSpawner.AllPlayers, true);

                    foreach (var _bossPlayer in bossPlayers)
                    {
                        botsGroup.RemoveEnemy(_bossPlayer);
                        botsGroup.AddAlly(_bossPlayer);
                    }

                    botSpawner.Groups.AddNoKey(botsGroup, botZone);

                    MiyakoCarryServicePlugin.Logger.LogInfo("Allies: " + string.Join(",", botsGroup.Allies.Select(player => player.Profile.Nickname)));
                    MiyakoCarryServicePlugin.Logger.LogInfo("Enemies: " + string.Join(",", botsGroup.Enemies.Keys.Select(player => player.Profile.Nickname)));
                    MiyakoCarryServicePlugin.Logger.LogInfo("Members: " + string.Join(",", botsGroup.Members.Select(player => player.Profile.Nickname)));

                    bossPlayer.BotsGroup = botsGroup;
                    bossPlayer.BotsGroup.Lock();

                    botOwner.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = false;
                    botOwner.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = oldReasons;

                    return botsGroup;
                    // }
                });

                botCreationDataClass.Profiles.ForEach(async profile =>
                {
                    var onActivate = new Action<BotOwner>((BotOwner botOwner) =>
                    {
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();

                        botSpawner.method_11(botOwner, botCreationDataClass, null, botCreationDataClass.SpawnParams.ShallBeGroup != null, stopWatch);

                        botOwner.Memory.DeleteInfoAboutEnemy(bossPlayer);

                        botOwner.GetPlayer.Physical.Stamina.ForceMode = true;
                        botOwner.GetPlayer.Physical.HandsStamina.ForceMode = true;

                        botOwner.GetPlayer.Profile.Info.GroupId = bossPlayer.Profile.Info.GroupId;
                        botOwner.GetPlayer.Profile.Info.TeamId = bossPlayer.Profile.Info.TeamId;

                        botOwner.BotFollower.PatrolDataFollower.InitPlayer(bossPlayer);
                        // botOwner.BotFollower.Index = Followers.Count - 1;
                        botOwner.BotFollower.BossToFollow = mcsAIBossPlayer;
                        var followerMode = PatrolMode.follower;
                        var simpleMode = PatrolMode.simple;
                        var pointChooser = PatrollingData.GetPointChooser(botOwner, simpleMode, botOwner.SpawnProfileData);
                        botOwner.PatrollingData.SetMode(followerMode, pointChooser);
                    });

                    botSpawner.InSpawnProcess += 1;

                    var cancellationToken = new CancellationToken();
                    await botCreator.ActivateBot(profile, new GClass682(bossPlayerPos, botCreationDataClass.GetPosition().CorePointId, true), closestZone, true, groupAction, onActivate, cancellationToken);
                });
            }
        }
    }
}