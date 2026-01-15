
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 从服务端获取所有要生成的护航Bot数据
    /// </summary>
    internal sealed class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsPresets), nameof(BotsPresets.TryLoadBotsProfilesOnStart));

        [PatchPostfix]
        public static async void Postfix(Task __result)
        {
            await __result;
            var csProfiles = await McsRequestHandler.GetCarryServicePlayer();
            foreach (var csProfile in csProfiles)
            {
                await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid, PoolManagerClass.AssemblyType.Online, csProfile.GetAllPrefabPaths(false).ToArray(), JobPriorityClass.General, new Progress<LoadingProgressStruct>(), default);
            }
            var myPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            var myPlayerPos = myPlayer.Position;
            var wildSpawnType = myPlayer.Side switch
            {
                EPlayerSide.Bear => WildSpawnType.pmcBEAR,
                EPlayerSide.Usec => WildSpawnType.pmcUSEC,
                _ => WildSpawnType.assault
            };

            myPlayer.Profile.Info.GroupId = myPlayer.Profile.Info.GroupId == "fika" ? "fika" : "mcs";

            var botSpawnParams = new BotSpawnParams();
            botSpawnParams.ShallBeGroup = new ShallBeGroupParams(true, false, 5);

            var botProfileDataClass = new BotProfileDataClass(myPlayer.Side, wildSpawnType, BotDifficulty.hard, 2, botSpawnParams);

            var botGame = Singleton<IBotGame>.Instance;
            var botsController = botGame.BotsController;
            var botSpawner = botsController.BotSpawner;
            var botCreator = botSpawner.BotCreator;

            var botCreationDataClass = await BotCreationDataClass.Create(botProfileDataClass, botCreator, 0, botSpawner);

            botCreationDataClass.AddProfiles(csProfiles.ToList());

            var closestGroupPoint = botsController.CoversData.GetClosest(myPlayerPos);
            botCreationDataClass.AddPosition(myPlayerPos, closestGroupPoint.CorePointInGame.Id);

            var closestZone = botSpawner.GetClosestZone(myPlayerPos, out _);

            var groupAction = new Func<BotOwner, BotZone, BotsGroup>((BotOwner botOwner, BotZone botZone) =>
            {
                // if (myPlayer.Side != EPlayerSide.Savage)
                // {

                if (myPlayer.BotsGroup != null)
                {
                    return myPlayer.BotsGroup;
                }

                botOwner.Settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = myPlayer.Side == EPlayerSide.Savage;
                botOwner.Settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = myPlayer.Side != EPlayerSide.Savage;

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

                var botsGroup = new BotsGroup(closestZone, botGame, botOwner, enemies.ToList(), botSpawner.DeadBodiesController, botSpawner.AllPlayers, false);

                botsGroup.RemoveEnemy(myPlayer);
                botsGroup.AddAlly(myPlayer);

                botSpawner.Groups.AddNoKey(botsGroup, botZone);

                MiyakoCarryServicePlugin.Logger.LogError("Allies: " + string.Join(",", botsGroup.Allies));
                MiyakoCarryServicePlugin.Logger.LogError("Enemies: " + string.Join(",", botsGroup.Enemies));
                MiyakoCarryServicePlugin.Logger.LogError($"GroupId: {botOwner.GroupId}");

                myPlayer.BotsGroup = botsGroup;
                myPlayer.BotsGroup.Lock();

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

                    botOwner.Memory.DeleteInfoAboutEnemy(myPlayer);

                    botOwner.GetPlayer.Physical.Stamina.ForceMode = true;
                    botOwner.GetPlayer.Physical.HandsStamina.ForceMode = true;

                    botOwner.GetPlayer.Profile.Info.GroupId = myPlayer.Profile.Info.GroupId;
                    botOwner.GetPlayer.Profile.Info.TeamId = myPlayer.Profile.Info.TeamId;
                });

                botSpawner.InSpawnProcess += 1;

                var cancellationToken = new CancellationToken();
                await botCreator.ActivateBot(profile, new GClass682(myPlayerPos, botCreationDataClass.GetPosition().CorePointId, true), closestZone, true, groupAction, onActivate, cancellationToken);
            });
        }
    }
}