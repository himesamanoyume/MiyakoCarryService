
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
            var botProfileDataClass = new BotProfileDataClass(myPlayer.Side, wildSpawnType, BotDifficulty.hard, 0);

            var botGame = Singleton<IBotGame>.Instance;
            var botsController = botGame.BotsController;
            var botSpawner = botsController.BotSpawner;
            var botCreator = botSpawner.BotCreator;

            var botSpawnParams = new BotSpawnParams();
            botSpawnParams.ShallBeGroup = new ShallBeGroupParams(true, false, 5);

            var botCreationDataClass = await BotCreationDataClass.Create(botProfileDataClass, botCreator, 0, botSpawner);

            botCreationDataClass.AddProfiles(csProfiles.ToList());

            var closestGroupPoint = botsController.CoversData.GetClosest(myPlayerPos);
            botCreationDataClass.AddPosition(myPlayerPos, closestGroupPoint.Id);

            var closestZone = botSpawner.GetClosestZone(myPlayerPos, out _);

            // 问题
            var groupAction = new Func<BotOwner, BotZone, BotsGroup>(botSpawner.GetGroupAndSetEnemies);
            // end

            botCreationDataClass.Profiles.ForEach(async profile =>
            {
                var onActivate = new Action<BotOwner>((BotOwner botOwner) =>
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    botSpawner.method_11(botOwner, botCreationDataClass, null, true, stopWatch);
                });

                botSpawner.InSpawnProcess += 1;

                var cancellationToken = new CancellationToken();
                await botCreator.ActivateBot(profile, new GClass682(myPlayerPos, botCreationDataClass.GetPosition().CorePointId, true), closestZone, true, groupAction, onActivate, cancellationToken);
            });
        }
    }
}