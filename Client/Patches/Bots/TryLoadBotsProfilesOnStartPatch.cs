
using System;
using System.Collections.Generic;
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
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Patches.Events;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 从服务端获取所有要生成的护航Bot数据
    /// </summary>
    internal sealed class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        private static SquadMgr SquadMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        private static SubTitleMgr SubTitleMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SubTitleMgr>();
            }
        }

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsPresets), nameof(BotsPresets.TryLoadBotsProfilesOnStart));

        [PatchPostfix]
        public static async void Postfix(Task __result)
        {
            await __result;
            var currentType = MatchmakerAcceptScreenShowPatch.CurrentType;
            var mcsProfilesDict = await McsRequestHandler.GetMcsBotPlayers(new()
            {
                Side = currentType
            });
            Dictionary<MongoID, McsBotPlayerConfig> mcsLeadPlayerConfigs;
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                mcsLeadPlayerConfigs = await McsRequestHandler.GetMcsBotPlayerConfigs();
            }
            else
            {
                mcsLeadPlayerConfigs = new();
            }

            foreach (var mcsProfileItem in mcsProfilesDict)
            {
                foreach (var mcsProfile in mcsProfileItem.Value)
                {
                    // MiyakoCarryServicePlugin.Logger.LogError(mcsProfile.ProfileId);
                    await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid, PoolManagerClass.AssemblyType.Online, mcsProfile.GetAllPrefabPaths(false).ToArray(), JobPriorityClass.General, new Progress<LoadingProgressStruct>(), default);
                }
            }

            var gameWorld = Singleton<GameWorld>.Instance;

            var leadPlayers = mcsProfilesDict
                .Where(kvp => kvp.Value.Length > 0)
                .Select(kvp => gameWorld.GetEverExistedPlayerByID(kvp.Key))
                .Where(leadPlayer => leadPlayer != null);

            var botGame = Singleton<IBotGame>.Instance;
            var botsController = botGame.BotsController;
            var botSpawner = botsController.BotSpawner;
            var botCreator = botSpawner.BotCreator;

            MiyakoCarryServicePlugin.Logger.LogInfo("leadPlayer Count: " + leadPlayers.Count());

            foreach (var leadPlayer in leadPlayers)
            {
                leadPlayer.BeingHitAction += (DamageInfoStruct damageInfo, EBodyPart bodyPart, float value) =>
                {
                    if (damageInfo.Player == null || !damageInfo.Player.IsAI || damageInfo.Player.AIData == null || damageInfo.Player.AIData.BotOwner == null)
                    {
                        return;
                    }

                    var enemyBotOwner = damageInfo.Player.AIData.BotOwner;

                    if (SquadMgr.IsMcsBotPlayer(enemyBotOwner.ProfileId))
                    {
                        return;
                    }

                    if (leadPlayer.BotsGroup != null)
                    {
                        leadPlayer.BotsGroup.AddEnemy(enemyBotOwner, EBotEnemyCause.AddEnemyToAllGroups);
                        leadPlayer.BotsGroup.ReportAboutEnemy(enemyBotOwner, EEnemyPartVisibleType.Sence, SquadMgr.GetAllMcsSquadMembersByMcsLeadId(leadPlayer.ProfileId).FirstOrDefault());
                    }
                };

                var leadPlayerPos = leadPlayer.Position;
                if (!mcsLeadPlayerConfigs.TryGetValue(leadPlayer.ProfileId, out var mcsBotPlayerConfig))
                {
                    mcsBotPlayerConfig = new McsBotPlayerConfig
                    {
                        McsLeadPlayerId = GameLoop.Instance.Session.Profile.Id,
                        PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
                        ArmorLevelThreshold = MiyakoCarryServicePlugin.ArmorLevelThreshold.Value,
                        LootingWishlishItem = MiyakoCarryServicePlugin.LootingWishlishItem.Value,
                        LootingQuestItem = MiyakoCarryServicePlugin.LootingQuestItem.Value,
                        BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value
                    };
                }
                var mcsAILeadPlayer = new McsAILeadPlayer(leadPlayer, mcsBotPlayerConfig);
                leadPlayer.Profile.Info.GroupId = leadPlayer.Profile.Info.GroupId == "fika" ? "fika" : "mcs";

                if (mcsProfilesDict[leadPlayer.ProfileId].Count() == 0)
                {
                    continue;
                }

                var mcsBotPlayerProfiles = mcsProfilesDict[leadPlayer.ProfileId].ToList();
                mcsBotPlayerProfiles.Sort((a, b) => b.Info.Level.CompareTo(a.Info.Level));

                foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
                {
                    if (SquadMgr.IsMcsBotPlayerDead(mcsBotPlayerProfile.ProfileId))
                    {
                        continue;
                    }

                    var wildSpawnType = mcsBotPlayerProfile.Info.Settings.Role;
                    var botDifficulty = mcsBotPlayerProfile.Info.Settings.BotDifficulty;

                    var botSpawnParams = new BotSpawnParams
                    {
                        ShallBeGroup = new ShallBeGroupParams(true, false, 5)
                    };

                    var botProfileDataClass = new BotProfileDataClass(leadPlayer.Side, wildSpawnType, botDifficulty, 2, botSpawnParams);

                    var botCreationDataClass = await BotCreationDataClass.Create(botProfileDataClass, botCreator, 0, botSpawner);

                    botCreationDataClass.AddProfile(mcsBotPlayerProfile);

                    var closestGroupPoint = botsController.CoversData.GetClosest(leadPlayerPos);
                    botCreationDataClass.AddPosition(leadPlayerPos, closestGroupPoint.CorePointInGame.Id);

                    var closestZone = botSpawner.GetClosestZone(leadPlayerPos, out _);

                    var groupAction = new Func<BotOwner, BotZone, BotsGroup>((BotOwner botOwner, BotZone botZone) =>
                    {
                        var settings = Singleton<GClass620>.Instance.GetSettings(botDifficulty, wildSpawnType, true);

                        settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = true;
                        settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = leadPlayer.Side != EPlayerSide.Savage;

                        // var oldReasons = settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY;

                        // settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = true;
                        // settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = [];
                        settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
                        settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
                        settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;

                        // - hardcode some settings to make the bot more efficient
                        settings.FileSettings.Move.REACH_DIST = 1.5f;
                        settings.FileSettings.Move.REACH_DIST_COVER = 2f;
                        settings.FileSettings.Move.REACH_DIST_RUN = 1.5f;

                        settings.FileSettings.Mind.PART_PERCENT_TO_HEAL = 0.9f;
                        settings.FileSettings.Mind.DIST_TO_STOP_RUN_ENEMY = 15f;
                        settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = 40f;
                        settings.FileSettings.Mind.TIME_TO_FIND_ENEMY = 30f;
                        settings.FileSettings.Mind.ATTACK_IMMEDIATLY_CHANCE_0_100 = 100f;
                        settings.FileSettings.Mind.CHANCE_TO_RUN_CAUSE_DAMAGE_0_100 = 50f;

                        settings.FileSettings.Mind.CAN_TALK = true;
                        settings.FileSettings.Mind.TALK_WITH_QUERY = true;
                        botOwner.BotTalk.CanSay = true;

                        // - fix missing phrases (bug appeared in 0.16)
                        if (!botOwner.BotTalk.Priority.Any(x => x.Key == EPhraseTrigger.Ready))
                        {
                            botOwner.BotTalk.Priority.Add(EPhraseTrigger.Ready, 140f);
                        }
                        if (!botOwner.BotTalk.Priority.Any(x => x.Key == EPhraseTrigger.Going))
                        {
                            botOwner.BotTalk.Priority.Add(EPhraseTrigger.Going, 141f);
                        }
                        if (!botOwner.BotTalk.Priority.Any(x => x.Key == EPhraseTrigger.DontKnow))
                        {
                            botOwner.BotTalk.Priority.Add(EPhraseTrigger.DontKnow, 142f);
                        }

                        // 此项如果不为false，就会导致SAIN无法进入Combat Layer
                        settings.FileSettings.Mind.CAN_STAND_BY = false;
                        settings.FileSettings.Mind.CAN_TAKE_ANY_ITEM = true;
                        settings.FileSettings.Mind.CAN_TAKE_ITEMS = true;
                        settings.FileSettings.Mind.CAN_THROW_REQUESTS = true;
                        settings.FileSettings.Mind.CAN_DROP_ITEMS = true; // prevent bot from dropping items randomly
                        settings.FileSettings.Mind.CAN_USE_MEDS = true;
                        settings.FileSettings.Mind.MEDS_ONLY_SAFE_CONTAINER = false;
                        settings.FileSettings.Mind.SURGE_KIT_ONLY_SAFE_CONTAINER = false;
                        settings.FileSettings.Mind.GROUP_ANY_PHRASE_DELAY = 2f;
                        settings.FileSettings.Mind.GROUP_EXACTLY_PHRASE_DELAY = 1f;
                        settings.FileSettings.Mind.GROUP_EXACTLY_PHRASE_DELAY_MAX = 1f;
                        settings.FileSettings.Mind.CHANCE_FUCK_YOU_ON_CONTACT_100 = 0f;
                        settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG = 360f;
                        settings.FileSettings.Mind.REVENGE_TO_GROUP = true;

                        // force follower loyality
                        settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_SAVAGE = leadPlayer.Side == EPlayerSide.Savage;
                        settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_BEAR = leadPlayer.Side == EPlayerSide.Bear;
                        settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_USEC = leadPlayer.Side == EPlayerSide.Usec;
                        settings.FileSettings.Mind.CAN_EXECUTE_REQUESTS = true;

                        settings.FileSettings.Mind.FRIEND_AGR_KILL = 0.3f;
                        settings.FileSettings.Mind.FRIEND_DEAD_AGR_LOW = -0.3f;
                        settings.FileSettings.Mind.REVENGE_FOR_SAVAGE_PLAYERS = true;

                        // follower can turn enemy to anyone and cares only for the boss
                        settings.GetWarnBotTypes().Clear();
                        settings.FileSettings.Mind.WARN_BOT_TYPES = [];
                        settings.FileSettings.Mind.REVENGE_BOT_TYPES = [];

                        settings.FileSettings.Mind.BULLET_FEEL_CLOSE_SDIST = 30f;
                        settings.FileSettings.Mind.DIST_TO_ENEMY_SPOTTED_ON_HIT = 200f;
                        settings.FileSettings.Mind.DOG_FIGHT_IN = 0f;
                        settings.FileSettings.Mind.DOG_FIGHT_OUT = 0f;
                        settings.FileSettings.Mind.SHOOT_INSTEAD_DOG_FIGHT = 0f;
                        settings.FileSettings.Mind.MIN_DAMAGE_SCARE = 10f;

                        settings.FileSettings.Patrol.PICKUP_ITEMS_TO_BACKPACK_OR_CONTAINER = true;
                        settings.FileSettings.Patrol.CHANCE_TO_PLAY_VOICE_WHEN_CLOSE = 50f;
                        settings.FileSettings.Patrol.CHANCE_TO_PLAY_GESTURE_WHEN_CLOSE = 100f;
                        settings.FileSettings.Patrol.CAN_PEACEFUL_LOOK = true;
                        settings.FileSettings.Patrol.FRIEND_SEARCH_SEC = 60f;
                        settings.FileSettings.Patrol.FOLLOWER_START_MOVE_DELAY = 0.5f;
                        settings.FileSettings.Patrol.CAN_FRIENDLY_TILT = true;
                        settings.FileSettings.Patrol.VISION_DIST_COEF_PEACE = 1f;
                        settings.FileSettings.Patrol.MAX_YDIST_TO_START_WARN_REQUEST_TO_REQUESTER = 0f;

                        settings.FileSettings.Boss.SHALL_WARN = false;
                        settings.FileSettings.Boss.BIG_PIPE_ARTILLERY_COUNT = 1;
                        settings.FileSettings.Boss.EFFECT_REGENERATION_PER_MIN = 40f;

                        settings.FileSettings.Core.CanGrenade = true;
                        settings.FileSettings.Core.CanRun = true;

                        settings.FileSettings.Cover.CHECK_CLOSEST_FRIEND = true;
                        settings.FileSettings.Cover.DOG_FIGHT_AFTER_LEAVE = 1f;
                        settings.FileSettings.Cover.HIDE_TO_COVER_TIME = 5f;
                        settings.FileSettings.Cover.HITS_TO_LEAVE_COVER = 2;
                        settings.FileSettings.Cover.HITS_TO_LEAVE_COVER_UNKNOWN = 2;
                        settings.FileSettings.Cover.TIME_TO_MOVE_TO_COVER = 15f;
                        settings.FileSettings.Cover.RETURN_TO_ATTACK_AFTER_AMBUSH_MIN = 20f;
                        settings.FileSettings.Cover.RETURN_TO_ATTACK_AFTER_AMBUSH_MAX = 50f;
                        settings.FileSettings.Cover.SPOTTED_GRENADE_RADIUS = 25f;
                        settings.FileSettings.Cover.SPOTTED_GRENADE_TIME = 7f;
                        settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING = true;

                        var botDifficultyInt = (int)botDifficulty;

                        // - faster aiming sett
                        settings.FileSettings.Aiming.COEF_IF_MOVE /= botDifficultyInt;
                        settings.FileSettings.Aiming.BOTTOM_COEF /= botDifficultyInt;
                        settings.FileSettings.Aiming.COEF_FROM_COVER /= botDifficultyInt;
                        settings.FileSettings.Aiming.PANIC_COEF /= botDifficultyInt;
                        settings.FileSettings.Aiming.MAX_AIMING_UPGRADE_BY_TIME /= botDifficultyInt * 2f;

                        // - improved shooting settings
                        settings.FileSettings.Aiming.SHPERE_FRIENDY_FIRE_SIZE = 0.5f;
                        settings.FileSettings.Aiming.AIMING_TYPE = 6; // the head is a priority

                        settings.FileSettings.Aiming.ANY_PART_SHOOT_TIME = 5f;
                        settings.FileSettings.Aiming.ANYTIME_LIGHT_WHEN_AIM_100 = 50f;
                        settings.FileSettings.Aiming.BAD_SHOOTS_MAX = 2;
                        settings.FileSettings.Aiming.BAD_SHOOTS_MIN = 0;
                        settings.FileSettings.Aiming.FIRST_CONTACT_ADD_CHANCE_100 = 20f;

                        // - hit disturbance settings
                        settings.FileSettings.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.2f;
                        settings.FileSettings.Aiming.BASE_HIT_AFFECTION_MAX_ANG = 10f;
                        settings.FileSettings.Aiming.BASE_HIT_AFFECTION_MIN_ANG = 2f;
                        settings.FileSettings.Aiming.DAMAGE_PANIC_TIME = 0f;
                        settings.FileSettings.Aiming.DAMAGE_TO_DISCARD_AIM_0_100 = 30f;

                        settings.FileSettings.Look.MINIMUM_VISIBLE_DIST = 300f;
                        settings.FileSettings.Look.CAN_USE_LIGHT = true;
                        settings.FileSettings.Look.NIGHT_VISION_ON = 100.0f;
                        settings.FileSettings.Look.NIGHT_VISION_OFF = 110.0f;
                        settings.FileSettings.Look.NIGHT_VISION_DIST = 160.0f;
                        settings.FileSettings.Look.VISIBLE_ANG_NIGHTVISION = 120f;
                        settings.FileSettings.Look.LOOK_THROUGH_PERIOD_BY_HIT = 5f;
                        settings.FileSettings.Look.LightOnVisionDistance = 50f;
                        settings.FileSettings.Look.LOOK_LAST_POSENEMY_IF_NO_DANGER_SEC = 25f;
                        settings.FileSettings.Look.VISIBLE_ANG_LIGHT = 55f;
                        settings.FileSettings.Look.VISIBLE_DISNACE_WITH_LIGHT = 80f;
                        settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR = 1.5f;
                        settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR_GREEN = 2f;
                        settings.FileSettings.Look.LOOK_THROUGH_GRASS = false;
                        settings.FileSettings.Look.DIST_REPEATED_SEEN = 100.0f;
                        settings.FileSettings.Look.MAX_VISION_GRASS_METERS = 0.01f;
                        settings.FileSettings.Look.MAX_VISION_GRASS_METERS_FLARE = 0.01f;
                        settings.FileSettings.Look.NO_GREEN_DIST = 100.0f;
                        settings.FileSettings.Look.NO_GRASS_DIST = 100.0f;
                        settings.FileSettings.Look.CHECK_HEAD_ANY_DIST = true;
                        settings.FileSettings.Look.MIDDLE_DIST_CAN_SHOOT_HEAD = true;

                        settings.FileSettings.Hearing.CHANCE_TO_HEAR_SIMPLE_SOUND_0_1 = 1f;
                        settings.FileSettings.Hearing.DISPERSION_COEF = 10f * botDifficultyInt;
                        settings.FileSettings.Hearing.DISPERSION_COEF_GUN = 100f + 20f * botDifficultyInt;
                        settings.FileSettings.Hearing.CLOSE_DIST = settings.FileSettings.Hearing.CLOSE_DIST * botDifficultyInt + 20f;
                        settings.FileSettings.Hearing.FAR_DIST += settings.FileSettings.Hearing.CLOSE_DIST + botDifficultyInt * 5f;
                        settings.FileSettings.Hearing.SOUND_DIR_DEEFREE *= botDifficultyInt;
                        settings.FileSettings.Hearing.LOOK_ONLY_DANGER = true;
                        settings.FileSettings.Hearing.HEAR_DELAY_WHEN_PEACE = 0.1f;
                        settings.FileSettings.Hearing.HEAR_DELAY_WHEN_HAVE_SMT = 0.1f;
                        settings.FileSettings.Hearing.RESET_TIMER_DIST = 5f;

                        settings.FileSettings.Shoot.WAIT_NEXT_SINGLE_SHOT = 0f;
                        settings.FileSettings.Shoot.WAIT_NEXT_SINGLE_SHOT_LONG_MAX = 2f - botDifficultyInt * 0.2f;
                        settings.FileSettings.Shoot.NEXT_SINGLE_SHOT_PAUSE = 0f;

                        settings.FileSettings.Grenade.NO_RUN_FROM_AI_GRENADES = false;

                        botOwner.ENEMY_LOOK_AT_ME = Mathf.Cos(settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG * 0.017453292f);
                        botOwner.GetPlayer.ActiveHealthController.SetDamageCoeff(settings.FileSettings.Core.DamageCoeff);

                        // counter SAIN
                        botOwner.LookSensor.ShootFromEyes = true;
                        settings.FileSettings.Look.SHOOT_FROM_EYES = true;

                        // - friendly bot never gets tired
                        // botOwner.GetPlayer.Physical.Stamina.ForceMode = true;
                        // botOwner.GetPlayer.Physical.HandsStamina.ForceMode = true;

                        // - need no food
                        // botOwner.GetPlayer.HealthController.DisableMetabolism();

                        botOwner.Tactic.AggressionChange(1f);

                        AccessTools.Field(typeof(LookSensor), "VISIBLE_ANGLE").SetValue(botOwner.LookSensor, Mathf.Cos(settings.FileSettings.Core.VisibleAngle * 0.017453292f));
                        AccessTools.Field(typeof(LookSensor), "VISIBLE_ANGLE_LIGHT").SetValue(botOwner.LookSensor, Mathf.Cos(settings.FileSettings.Look.VISIBLE_ANG_LIGHT * 0.017453292f));
                        AccessTools.Field(typeof(LookSensor), "VISIBLE_ANGLE_NIGHTVISION").SetValue(botOwner.LookSensor, Mathf.Cos(settings.FileSettings.Look.VISIBLE_ANG_NIGHTVISION * 0.017453292f));

                        botOwner.Settings = settings;

                        SquadMgr.AddMcsSquadMember(leadPlayer.ProfileId, botOwner.ProfileId, botOwner, mcsAILeadPlayer);
                        SubTitleMgr.CreateSubTitle(botOwner.ProfileId);

                        if (leadPlayer.BotsGroup != null)
                        {
                            // botOwner.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = false;
                            // botOwner.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = oldReasons;
                            
                            botOwner.Boss.IamBoss = false;
                            leadPlayer.BotsGroup.AddMember(botOwner, false);
                            return leadPlayer.BotsGroup;
                        }

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

                        botsGroup.OnReportEnemy += (IPlayer enemy, Vector3 enemyPos, Vector3 weaponRootLast, EEnemyPartVisibleType isVisibleOnlyBySense, BotOwner reporter) =>
                        {
                            if (enemy.ProfileId == leadPlayer.ProfileId)
                            {
                                return;
                            }
                            botsGroup.CheckAndAddEnemy(enemy);
                        };

                        foreach (var _leadPlayer in leadPlayers)
                        {
                            botsGroup.RemoveEnemy(_leadPlayer);
                            botsGroup.AddAlly(_leadPlayer);
                        }

                        botSpawner.Groups.AddNoKey(botsGroup, botZone);
                        botsGroup.AddMember(botOwner, false);

                        leadPlayer.BotsGroup = botsGroup;
                        leadPlayer.BotsGroup.Lock();

                        // botOwner.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = false;
                        // botOwner.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = oldReasons;

                        return botsGroup;
                    });

                    botCreationDataClass.Profiles.ForEach(async profile =>
                    {
                        var onActivate = new Action<BotOwner>((BotOwner botOwner) =>
                        {
                            var stopWatch = new Stopwatch();
                            stopWatch.Start();

                            botSpawner.method_11(botOwner, botCreationDataClass, null, botCreationDataClass.SpawnParams.ShallBeGroup != null, stopWatch);

                            botOwner.Memory.DeleteInfoAboutEnemy(leadPlayer);

                            botOwner.GetPlayer.Physical.Stamina.ForceMode = true;
                            botOwner.GetPlayer.Physical.HandsStamina.ForceMode = true;

                            botOwner.GetPlayer.Profile.Info.GroupId = leadPlayer.Profile.Info.GroupId;
                            botOwner.GetPlayer.Profile.Info.TeamId = leadPlayer.Profile.Info.TeamId;

                            botOwner.BotFollower.PatrolDataFollower.InitPlayer(leadPlayer);
                            var pointChooser = PatrollingData.GetPointChooser(botOwner, PatrolMode.bossRoundProtectWithStay, botOwner.SpawnProfileData);
                            botOwner.PatrollingData.SetMode(PatrolMode.follower, pointChooser);
                            botOwner.BotFollower.BossToFollow = mcsAILeadPlayer;
                        });

                        botSpawner.InSpawnProcess += 1;

                        var cancellationToken = new CancellationToken();
                        await botCreator.ActivateBot(profile, new GClass682(leadPlayerPos, botCreationDataClass.GetPosition().CorePointId, true), closestZone, true, groupAction, onActivate, cancellationToken);
                    });
                }

            }
        }
    }
}