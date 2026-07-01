using Comfort.Common;
using EFT;
using System;
using SPT.Reflection.Utils;
using MiyakoCarryService.Client.Utils;
using System.Collections.Generic;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Mgrs;
using UnityEngine;
using System.IO;
using System.Reflection;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Misc;
using System.Threading.Tasks;
using MiyakoCarryService.Client.Patches.Events;
using System.Linq;
using MiyakoCarryService.Client.Models;
using System.Diagnostics;
using System.Threading;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Bots.Brain.Layers;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client
{
    public sealed class GameLoop : MiyakoCarryServiceSingleton<GameLoop>
    {
        public Dictionary<Type, IMgr> Mgrs { get; private set; } = new();
        public Dictionary<string, TraderOffer> ItemBestPriceDict { get; private set; } = new();
        public Shader HighlightShader { get; private set; } = null;
        public Camera MainCamera { get; private set; } = null;
        public Camera OpticCamera { get; private set; } = null;
        public bool IsGameStarted = false;
        private Debouncer<ItemData, McsAILeadPlayer> _updateDebouncer;
        private HashSet<MongoID> _loadedMcsLeadPlayer = new();

        public ISession Session
        {
            get
            {
                return field ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();
            }
        }

        public bool IsVaildGameWorld = false;

        public bool CheckVaildGameWorld()
        {
            IsVaildGameWorld = Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld && IsGameStarted;
            return IsVaildGameWorld;
        }

        void Update()
        {
            CheckVaildGameWorld();

            KeyInput.KeyDown(MiyakoCarryServicePlugin.EnableLootingHotKey.Value, MiyakoCarryServicePlugin.EnableLooting);

            if (!IsVaildGameWorld)
            {
                return;
            }

            if (MainCamera == null)
            {
                MainCamera = Camera.main;
            }

            if (OpticCamera == null)
            {
                foreach (var camera in Camera.allCameras)
                {
                    if (camera.name == "BaseOpticCamera(Clone)")
                    {
                        OpticCamera = camera;
                        break;
                    }
                }
            }
        }

        public void LoadAssetBundle()
        {
            if (HighlightShader != null)
            {
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MiyakoCarryService.Client.Assets.miyakocarryservice";
            var highlightShaderName = "assets/shader/teammatehighlight.shader";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return;
                }

                byte[] assetBytes;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    assetBytes = memoryStream.ToArray();
                }

                AssetBundle bundle = AssetBundle.LoadFromMemory(assetBytes);
                if (bundle != null)
                {
                    HighlightShader = bundle.LoadAsset<Shader>(highlightShaderName);
                    if (HighlightShader == null)
                    {
                        UnityEngine.Debug.LogException(new Exception($"无法加载Shader: {highlightShaderName}"));
                    }

                    bundle.Unload(false);
                    return;
                }
            }
        }

        public void Init()
        {
            LoadAssetBundle();

            McsMgr.Enable();
            BrainMgr.Enable();
            PlayerDataMgr.Enable();
            LootDataMgr.Enable();
            SubtitlesMgr.Enable();
            CommandMgr.Enable();
            HighlightMgr.Enable();
            ExfilDataMgr.Enable();
            TransitDataMgr.Enable();
            QuestDataMgr.Enable();
            SwitchDataMgr.Enable();
            TripwireDataMgr.Enable();
            BarbedWireDataMgr.Enable();
            BorderZoneDataMgr.Enable();
            DamageTriggerDataMgr.Enable();
            RoomTrapDataMgr.Enable();
            DoorDataMgr.Enable();

            EventMgr.Subscribe<GameWorldStartedEvent>(OnGameWorldStarted, this);
            EventMgr.Subscribe<GameWorldEndedEvent>(OnGameWorldEnded, this);
            EventMgr.Subscribe<UpdateProfileEvent>(OnUpdateProfile, this);
            EventMgr.Subscribe<UpdateDailyQuestsEvent>(OnUpdateDailyQuests, this);
            EventMgr.Subscribe<UpdateMiyakoTraderAssortmentEvent>(OnUpdateMiyakoTraderAssortment, this);
        }

        public void OnUpdateProfile(UpdateProfileEvent @event)
        {
            TasksExtensions.HandleExceptions(UpdateProfile());
        }

        public void OnUpdateDailyQuests(UpdateDailyQuestsEvent @event)
        {
            TasksExtensions.HandleExceptions(UpdateDailyQuests());
        }

        public void OnUpdateMiyakoTraderAssortment(UpdateMiyakoTraderAssortmentEvent @event)
        {
            TasksExtensions.HandleExceptions(UpdateMiyakoTraderAssortmentEvent());
        }

        private async Task UpdateDailyQuests()
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(1000);
                TarkovApplication.Exist(out var tarkovApplication);
                var tarkovApplicationTraverse = Traverse.Create(tarkovApplication);

                var mainMenuControllerClass = tarkovApplicationTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value;
                var array = await Session.GetDailyQuests();
                if (!array.IsNullOrEmpty())
                {
                    if (mainMenuControllerClass == null)
                    {
                        return;
                    }

                    if (mainMenuControllerClass.LocalQuestControllerClass == null)
                    {
                        return;
                    }

                    if (mainMenuControllerClass.LocalQuestControllerClass.QuestBookClass == null)
                    {
                        return;
                    }
                    mainMenuControllerClass.LocalQuestControllerClass.QuestBookClass.UpdateDailyQuests(array);
                }
            }
        }

        private async Task UpdateMiyakoTraderAssortmentEvent()
        {
            foreach (var trader in Session.Traders)
            {
                if (trader.Id == MiyakoCarryServicePlugin.MiyakoTraderId)
                {
                    TasksExtensions.HandleExceptions(trader.RefreshAssortment(true, true));
                    break;
                }
            }
        }

        private async Task UpdateProfile()
        {
            var profileChangesPocoClass = await McsRequestHandler.UpdateProfile();
            if (Session is SessionBackendClass sessionBackendClass)
            {
                var profile = sessionBackendClass.Profile;
                var correctInfo = profile.Info;

                foreach (var (traderId, traderInfo) in profile.TradersInfo)
                {
                    if (traderInfo.ProfileInfo != correctInfo)
                    {
                        traderInfo.ProfileInfo = correctInfo;
                    }
                }

                if (sessionBackendClass.Dictionary_0.TryGetValue(sessionBackendClass.Profile.Id, out var profileUpdater))
                {
                    profileUpdater.UpdateProfile(profileChangesPocoClass);
                }
            }
        }

        private void OnGameWorldStarted(GameWorldStartedEvent @event)
        {
            Reset();
        }

        private void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            Reset();
            if (_updateDebouncer != null)
            {
                _updateDebouncer.Flush();
                _updateDebouncer.Clear();
            }
            _updateDebouncer = null;
            if (_loadedMcsLeadPlayer != null)
            {
                _loadedMcsLeadPlayer.Clear();
            }
        }

        private void Reset()
        {
            MainCamera = null;
            OpticCamera = null;
        }

        public override void Destroy()
        {
            EventMgr.UnsubscribeAll(this);
            Reset();
            if (_updateDebouncer != null)
            {
                _updateDebouncer.Flush();
                _updateDebouncer.Clear();
                _updateDebouncer = null;
            }
            if (_loadedMcsLeadPlayer != null)
            {
                _loadedMcsLeadPlayer.Clear();
                _loadedMcsLeadPlayer = null;
            }
            HighlightShader = null;
            if (Mgrs != null)
            {
                foreach (var mgr in Mgrs.Values)
                {
                    mgr.OnMgrDestroy();
                }
                Mgrs.Clear();
            }
            Mgrs = null;
            if (ItemBestPriceDict != null)
            {
                ItemBestPriceDict.Clear();
            }
            ItemBestPriceDict = null;
            base.Destroy();
        }

        public T GetMgr<T>() where T : IMgr
        {
            return (T)Mgrs[typeof(T)];
        }

        public HashSet<T> GetDatas<T, K>() where T : BaseData where K : DataMgr<K>
        {
            var mgr = GetMgr<K>();
            return mgr.GetDatas<T>();
        }

        public void DebouncedRefresh(ItemData itemData, McsAILeadPlayer mcsAILeadPlayer)
        {
            if (_updateDebouncer == null)
            {
                _updateDebouncer = new Debouncer<ItemData, McsAILeadPlayer>(
                    this,
                    1f,
                    BatchRefreshItems
                );
            }

            if (_updateDebouncer != null && itemData != null)
            {
                _updateDebouncer.Trigger(itemData, mcsAILeadPlayer);
            }
        }

        private void BatchRefreshItems(Dictionary<ItemData, McsAILeadPlayer> updates)
        {
            int batchSize = Mathf.Clamp(Mathf.CeilToInt(updates.Count / 10f), 5, 300);
            var batches = new List<List<KeyValuePair<ItemData, McsAILeadPlayer>>>();
            var currentBatch = new List<KeyValuePair<ItemData, McsAILeadPlayer>>();

            foreach (var kvp in updates)
            {
                currentBatch.Add(kvp);
                if (currentBatch.Count >= batchSize)
                {
                    batches.Add(currentBatch);
                    currentBatch.Clear();
                }
            }

            if (currentBatch.Count > 0)
            {
                batches.Add(currentBatch);
            }

            foreach (var batch in batches)
            {
                foreach (var kvp in batch)
                {
                    try
                    {
                        StartCoroutine(kvp.Key.UnlockRefreshRootItemInteresting(kvp.Value));
                    }
                    catch (Exception e)
                    {
                        MiyakoCarryServicePlugin.Logger.LogError($"Batch refresh item error: {e}");
                    }
                }
            }
        }

        public async Task SpawnMcsBotPlayer()
        {
            var currentType = MatchmakerAcceptScreenShowPatch.CurrentType;
            var mcsProfilesDict = await McsRequestHandler.GetMcsBotPlayers(new()
            {
                Side = currentType
            });

            if (mcsProfilesDict.Count == 0)
            {
                return;
            }

            var mcsMgr = MgrAccessor.Get<McsMgr>();
            var subtitlesMgr = MgrAccessor.Get<SubtitlesMgr>();

            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                mcsMgr.McsLeadPlayerConfigs = await McsRequestHandler.GetMcsBotPlayerConfigs();
            }
            else
            {
                mcsMgr.McsLeadPlayerConfigs = new();
            }

            foreach (var mcsProfileItem in mcsProfilesDict)
            {
                foreach (var mcsProfile in mcsProfileItem.Value)
                {
                    await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid, PoolManagerClass.AssemblyType.Local, mcsProfile.GetAllPrefabPaths(false).ToArray(), JobPriorityClass.Immediate, new Progress<LoadingProgressStruct>(), default);
                }
            }

            var gameWorld = Singleton<GameWorld>.Instance;

            var leadPlayers = mcsProfilesDict
                .Where(kvp => kvp.Value.Length > 0)
                .Select(kvp => gameWorld.GetEverExistedPlayerByID(kvp.Key))
                .Where(leadPlayer => leadPlayer != null);

            var botGame = Singleton<IBotGame>.Instance;
            if (botGame == null)
            {
                return;
            }

            var botsController = botGame.BotsController;
            if (botsController == null)
            {
                return;
            }

            var botSpawner = botsController.BotSpawner;
            if (botSpawner == null)
            {
                return;
            }

            var botCreator = botSpawner.BotCreator;
            if (botCreator == null)
            {
                return;
            }

            foreach (var leadPlayer in leadPlayers)
            {
                if (_loadedMcsLeadPlayer.Contains(leadPlayer.ProfileId))
                {
                    continue;
                }

                _loadedMcsLeadPlayer.Add(leadPlayer.ProfileId);

                leadPlayer.BeingHitAction += (DamageInfoStruct damageInfo, EBodyPart bodyPart, float value) =>
                {
                    if (damageInfo.Player?.AIData?.BotOwner == null)
                    {
                        return;
                    }

                    var enemyBotOwner = damageInfo.Player.AIData.BotOwner;

                    if (mcsMgr.IsMcsBotPlayer(enemyBotOwner.ProfileId))
                    {
                        return;
                    }

                    if (leadPlayer.BotsGroup != null)
                    {
                        var mcsBotPlayers = mcsMgr.GetAllMcsSquadMembersByMcsLeadId(leadPlayer.ProfileId);

                        if (mcsBotPlayers == null)
                        {
                            return;
                        }

                        var mcsBotPlayer = mcsBotPlayers.FirstOrDefault();
                        if (mcsBotPlayer?.AIData?.BotOwner?.BotFollower?.BossToFollow is McsAILeadPlayer mcsAILeadPlayer)
                        {
                            mcsAILeadPlayer.CalcGoalEnemy();
                        }
                    }
                };

                var leadPlayerPos = leadPlayer.Position;
                if (!mcsMgr.McsLeadPlayerConfigs.TryGetValue(leadPlayer.ProfileId, out var mcsBotPlayerConfig))
                {
                    mcsBotPlayerConfig = new McsBotPlayerConfig
                    {
                        McsLeadPlayerId = leadPlayer.ProfileId,
                        EnableLooting = MiyakoCarryServicePlugin.EnableLooting.Value,
                        PriceThreshold = MiyakoCarryServicePlugin.PriceThreshold.Value,
                        KeywordItemText = MiyakoCarryServicePlugin.KeywordItemText.Value,
                        LootingKeywordItem = MiyakoCarryServicePlugin.LootingKeywordItem.Value,
                        BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value
                    };
                    mcsMgr.UpdateMcsBotPlayerConfig(mcsBotPlayerConfig.McsLeadPlayerId, mcsBotPlayerConfig);
                }
                var mcsAILeadPlayer = new McsAILeadPlayer(leadPlayer);
                leadPlayer.Profile.Info.GroupId = leadPlayer.Profile.Info.GroupId == "Fika" ? "Fika" : "Mcs";

                if (mcsProfilesDict[leadPlayer.ProfileId].Count() == 0)
                {
                    continue;
                }

                var mcsBotPlayerProfiles = mcsProfilesDict[leadPlayer.ProfileId].ToList();
                mcsBotPlayerProfiles.Sort((a, b) => b.Info.Level.CompareTo(a.Info.Level));

                foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
                {
                    if (mcsMgr.IsMcsBotPlayerDead(mcsBotPlayerProfile.ProfileId))
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
                        botOwner.GetPlayer.Profile.Info.GroupId = leadPlayer.Profile.Info.GroupId;
                        botOwner.GetPlayer.Profile.Info.TeamId = leadPlayer.Profile.Info.TeamId;

                        var settings = SetBotSettings(botDifficulty, wildSpawnType, botOwner, leadPlayer);

                        botOwner.Settings = settings;

                        mcsMgr.AddMcsSquadMember(leadPlayer.ProfileId, botOwner.ProfileId, mcsAILeadPlayer);
                        subtitlesMgr.CreateSubTitle(botOwner.Profile);

                        if (leadPlayer.BotsGroup != null)
                        {
                            botOwner.Boss.IamBoss = false;
                            leadPlayer.BotsGroup.AddMember(botOwner, false);
                            return leadPlayer.BotsGroup;
                        }

                        var enemyTypes = botOwner.Settings.GetEnemyBotTypes();

                        if (leadPlayer.Side != EPlayerSide.Savage)
                        {
                            foreach (WildSpawnType wst in Enum.GetValues(typeof(WildSpawnType)))
                            {
                                enemyTypes.Add(wst);
                            }
                        }
                        if (!enemyTypes.Contains(WildSpawnType.pmcBEAR))
                        {
                            enemyTypes.Add(WildSpawnType.pmcBEAR);
                        }
                        if (!enemyTypes.Contains(WildSpawnType.pmcUSEC))
                        {
                            enemyTypes.Add(WildSpawnType.pmcUSEC);
                        }

                        botOwner.Settings.GetAlwaysFriendlyBotTypes().Clear();
                        botOwner.Settings.GetFriendNoWarnBotTypes().Clear();
                        botOwner.Settings.GetWarnBotTypes().Clear();

                        var enemies = botSpawner.method_5(botOwner);

                        var botsGroup = new BotsGroup(closestZone, botGame, botOwner, enemies.ToList(), botSpawner.DeadBodiesController, botSpawner.AllPlayers, true);

                        var notScav = leadPlayer.Side != EPlayerSide.Savage;
                        if (notScav)
                        {
                            botsGroup.OnReportEnemy += (IPlayer enemy, Vector3 enemyPos, Vector3 weaponRootLast, EEnemyPartVisibleType isVisibleOnlyBySense, BotOwner reporter) =>
                            {
                                if (enemy.Profile.Info.GroupId is "Mcs" or "Fika" || mcsMgr.IsMcsLeadPlayer(enemy.ProfileId) || mcsMgr.IsMcsBotPlayer(enemy.ProfileId))
                                {
                                    return;
                                }
                                botsGroup.AddEnemy(enemy, EBotEnemyCause.byKill);
                            };
                        }

                        foreach (var _leadPlayer in leadPlayers)
                        {
                            botsGroup.RemoveEnemy(_leadPlayer);
                            botsGroup.AddAlly(_leadPlayer);
                        }

                        botSpawner.Groups.AddNoKey(botsGroup, botZone);
                        botsGroup.AddMember(botOwner, false);

                        leadPlayer.BotsGroup = botsGroup;
                        leadPlayer.BotsGroup.Lock();

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

                            void InjectLayers(BaseBrain baseBrain = null)
                            {
                                BigBrainUtils.McsRemoveLayers(baseBrain.Owner, Classification.RemoveLayerNames);
                                BigBrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsCommonLayer), 65);
                                BigBrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsEscortLayer), 66);
                                BigBrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsAvoidDangerLayer), 67);
                                BigBrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsProxyLayer), 68);
                                BigBrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsFightLayer), baseBrain.ShortName() == nameof(EBrainName.BossZryachiy) || baseBrain.ShortName() == nameof(EBrainName.BossZryachiy) ? 186 : 88);
                                BigBrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsExfiltrationLayer), 89);
                            }

                            if (botOwner.Brain.BaseBrain != null)
                            {
                                InjectLayers();
                            }
                            else
                            {
                                Action<BaseBrain> handler = null;
                                handler = (BaseBrain b) =>
                                {
                                    botOwner.Brain.OnSetStrategy -= handler;
                                    InjectLayers(b);
                                };
                                botOwner.Brain.OnSetStrategy += handler;
                            }

                            if (!MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction)
                            {
                                return;
                            }

                            var slots = InventoryEquipment.AllSlotNames
                                .Where(slotName => slotName is not EquipmentSlot.Dogtag)
                                .Select(botOwner.Profile.Inventory.Equipment.GetSlot)
                                .ToArray();

                            foreach (var slot in slots)
                            {
                                if (slot.ContainedItem == null)
                                {
                                    continue;
                                }

                                var allItems = slot.ContainedItem.GetAllItems();
                                foreach (var item in allItems)
                                {
                                    var itemData = item.GetData();
                                    if (itemData == null)
                                    {
                                        continue;
                                    }

                                    if (itemData is not LootData lootData)
                                    {
                                        continue;
                                    }

                                    if (lootData.ItemType is EItemType.Backpack or EItemType.Equipment)
                                    {
                                        continue;
                                    }

                                    lootData.VanishingCurse = true;
                                }
                            }
                        });

                        botSpawner.InSpawnProcess += 1;

                        var cancellationToken = new CancellationToken();
                        await botCreator.ActivateBot(profile, new GClass682(leadPlayerPos, botCreationDataClass.GetPosition().CorePointId, true), closestZone, true, groupAction, onActivate, cancellationToken);
                    });
                }
            }
        }



        private BotDifficultySettingsClass SetBotSettings(BotDifficulty botDifficulty, WildSpawnType wildSpawnType, BotOwner botOwner, Player leadPlayer)
        {
            var settings = Singleton<GClass620>.Instance.GetSettings(botDifficulty, wildSpawnType, false);

            var notScav = leadPlayer.Side != EPlayerSide.Savage;

            settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = true;
            settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = notScav;

            settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = false;
            settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = notScav ? EWarnBehaviour.AlwaysEnemies : EWarnBehaviour.Neutral;
            settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
            settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;

            settings.FileSettings.Move.REACH_DIST = 1.5f;
            settings.FileSettings.Move.REACH_DIST_COVER = 2f;
            settings.FileSettings.Move.REACH_DIST_RUN = 1.5f;

            settings.FileSettings.Mind.PART_PERCENT_TO_HEAL = 1f;
            settings.FileSettings.Mind.DIST_TO_STOP_RUN_ENEMY = 15f;
            settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = 10f;
            settings.FileSettings.Mind.TIME_TO_FIND_ENEMY = 20f;
            settings.FileSettings.Mind.ATTACK_IMMEDIATLY_CHANCE_0_100 = 100f;
            settings.FileSettings.Mind.CHANCE_TO_RUN_CAUSE_DAMAGE_0_100 = 50f;

            settings.FileSettings.Mind.CAN_TALK = true;
            settings.FileSettings.Mind.TALK_WITH_QUERY = true;
            botOwner.BotTalk.CanSay = true;

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
            // end
            settings.FileSettings.Mind.CAN_TAKE_ANY_ITEM = true;
            settings.FileSettings.Mind.CAN_TAKE_ITEMS = true;
            settings.FileSettings.Mind.CAN_THROW_REQUESTS = true;
            settings.FileSettings.Mind.CAN_DROP_ITEMS = true;
            settings.FileSettings.Mind.CAN_USE_MEDS = true;
            settings.FileSettings.Mind.MEDS_ONLY_SAFE_CONTAINER = false;
            settings.FileSettings.Mind.SURGE_KIT_ONLY_SAFE_CONTAINER = false;
            settings.FileSettings.Mind.GROUP_ANY_PHRASE_DELAY = 2f;
            settings.FileSettings.Mind.GROUP_EXACTLY_PHRASE_DELAY = 1f;
            settings.FileSettings.Mind.GROUP_EXACTLY_PHRASE_DELAY_MAX = 1f;
            settings.FileSettings.Mind.CHANCE_FUCK_YOU_ON_CONTACT_100 = 0f;
            settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG = 360f;
            settings.FileSettings.Mind.REVENGE_TO_GROUP = true;
            settings.FileSettings.Mind.IGNORE_TRAP = false;
            settings.FileSettings.Mind.CHANCE_TO_IGNORE_TRIPWIRE = 0f;
            settings.FileSettings.Mind.CHACE_TO_DEACTIVATE = 100f;

            settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_SAVAGE = leadPlayer.Side == EPlayerSide.Savage;
            settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_BEAR = leadPlayer.Side == EPlayerSide.Bear;
            settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_USEC = leadPlayer.Side == EPlayerSide.Usec;
            settings.FileSettings.Mind.CAN_EXECUTE_REQUESTS = true;

            settings.FileSettings.Mind.FRIEND_AGR_KILL = 0.00003f;
            settings.FileSettings.Mind.FRIEND_DEAD_AGR_LOW = -0.00003f;
            settings.FileSettings.Mind.REVENGE_FOR_SAVAGE_PLAYERS = true;

            botOwner.Settings.GetAlwaysFriendlyBotTypes().Clear();
            botOwner.Settings.GetFriendNoWarnBotTypes().Clear();
            botOwner.Settings.GetWarnBotTypes().Clear();
            settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = [];
            settings.FileSettings.Mind.WARN_BOT_TYPES = [];
            settings.FileSettings.Mind.REVENGE_BOT_TYPES = [];

            settings.FileSettings.Mind.BULLET_FEEL_CLOSE_SDIST = 30f;
            settings.FileSettings.Mind.DIST_TO_ENEMY_SPOTTED_ON_HIT = 200f;
            settings.FileSettings.Mind.DOG_FIGHT_IN = 0f;
            settings.FileSettings.Mind.DOG_FIGHT_OUT = 0f;
            settings.FileSettings.Mind.SHOOT_INSTEAD_DOG_FIGHT = 0f;
            settings.FileSettings.Mind.MIN_DAMAGE_SCARE = 10f;
            settings.FileSettings.Mind.AVOID_BTR_RADIUS_SQR = 1f;

            settings.FileSettings.Patrol.PICKUP_ITEMS_TO_BACKPACK_OR_CONTAINER = true;
            settings.FileSettings.Patrol.CHANCE_TO_PLAY_VOICE_WHEN_CLOSE = 50f;
            settings.FileSettings.Patrol.CHANCE_TO_PLAY_GESTURE_WHEN_CLOSE = 100f;
            settings.FileSettings.Patrol.CAN_PEACEFUL_LOOK = true;
            settings.FileSettings.Patrol.FRIEND_SEARCH_SEC = 30f;
            settings.FileSettings.Patrol.FOLLOWER_START_MOVE_DELAY = 0.5f;
            settings.FileSettings.Patrol.CAN_FRIENDLY_TILT = true;
            settings.FileSettings.Patrol.VISION_DIST_COEF_PEACE = 1f;
            settings.FileSettings.Patrol.MAX_YDIST_TO_START_WARN_REQUEST_TO_REQUESTER = 0f;
            settings.FileSettings.Patrol.DO_RANDOM_DROP_ITEM = false;

            settings.FileSettings.Boss.SHALL_WARN = false;
            settings.FileSettings.Boss.BIG_PIPE_ARTILLERY_COUNT = 1;
            settings.FileSettings.Boss.EFFECT_REGENERATION_PER_MIN = 10f;

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
            settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING = false;

            var botDifficultyInt = (int)botDifficulty + 1;
            var aimingDifficultyMultiplier = botDifficulty switch
            {
                BotDifficulty.easy => 1.0f,
                BotDifficulty.normal => 0.75f,
                BotDifficulty.hard => 0.5f,
                BotDifficulty.impossible => 0.25f,
                _ => 1.0f
            };

            settings.FileSettings.Aiming.MAX_AIM_PRECICING = 60f;
            settings.FileSettings.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 1f * aimingDifficultyMultiplier;
            settings.FileSettings.Aiming.BOTTOM_COEF = 1f * aimingDifficultyMultiplier;
            settings.FileSettings.Aiming.MAX_AIM_TIME = 0.2f;
            settings.FileSettings.Aiming.COEF_FROM_COVER = 1f * aimingDifficultyMultiplier;
            settings.FileSettings.Aiming.HARD_AIM = 0.2f;
            settings.FileSettings.Aiming.HARD_AIM_CHANCE_100 = 100;
            settings.FileSettings.Aiming.PANIC_TIME = 0f;
            settings.FileSettings.Aiming.DAMAGE_PANIC_TIME = 0f;
            settings.FileSettings.Aiming.PANIC_COEF = 1f * aimingDifficultyMultiplier;
            settings.FileSettings.Aiming.PANIC_ACCURATY_COEF = 1f;
            settings.FileSettings.Aiming.DAMAGE_TO_DISCARD_AIM_0_100 = 0f;
            settings.FileSettings.Aiming.MIN_TIME_DISCARD_AIM_SEC = 0f;
            settings.FileSettings.Aiming.MAX_TIME_DISCARD_AIM_SEC = 0f;
            settings.FileSettings.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0f;
            settings.FileSettings.Aiming.BASE_HIT_AFFECTION_MIN_ANG = 0f;
            settings.FileSettings.Aiming.BASE_HIT_AFFECTION_MAX_ANG = 0f;
            settings.FileSettings.Aiming.SCATTERING_HAVE_DAMAGE_COEF = 0f;
            settings.FileSettings.Aiming.XZ_COEF = 0f;
            settings.FileSettings.Aiming.SCATTERING_DIST_MODIF = 0.1f;
            settings.FileSettings.Aiming.SCATTERING_DIST_MODIF_CLOSE = 0.1f;
            settings.FileSettings.Aiming.BASE_SHIEF = 0.1f;
            settings.FileSettings.Aiming.COEF_IF_MOVE = 1f * aimingDifficultyMultiplier;
            settings.FileSettings.Aiming.TIME_COEF_IF_MOVE = 1f;
            settings.FileSettings.Aiming.BOT_MOVE_IF_DELTA = 0.01f;
            settings.FileSettings.Aiming.AIMING_TYPE = 6;
            settings.FileSettings.Aiming.DIST_TO_SHOOT_TO_CENTER = 0f;
            settings.FileSettings.Aiming.DIST_TO_SHOOT_NO_OFFSET = 0f;
            settings.FileSettings.Aiming.SHOOT_TO_CHANGE_PRIORITY = 5525;
            settings.FileSettings.Aiming.FIRST_CONTACT_ADD_SEC = 0f;
            settings.FileSettings.Aiming.FIRST_CONTACT_ADD_CHANCE_100 = 0f;
            settings.FileSettings.Aiming.MISS_FIRST_SOOTS = 0;
            settings.FileSettings.Aiming.MISS_ON_START = 0;
            settings.FileSettings.Aiming.MISS_DIST = 500f;
            settings.FileSettings.Aiming.NEXT_SHOT_MISS_CHANCE_100 = 0f;
            settings.FileSettings.Aiming.NEXT_SHOT_MISS_Y_OFFSET = 1f;
            settings.FileSettings.Aiming.SHPERE_FRIENDY_FIRE_SIZE = 0.5f;
            settings.FileSettings.Aiming.WEAPON_ROOT_OFFSET = 0.35f;
            settings.FileSettings.Aiming.DANGER_UP_POINT = 3f;
            settings.FileSettings.Aiming.OFFSET_RECAL_ANYWAY_TIME = 1f;
            settings.FileSettings.Aiming.ANY_PART_SHOOT_TIME = 900f;
            settings.FileSettings.Aiming.ANYTIME_LIGHT_WHEN_AIM_100 = 100f;
            settings.FileSettings.Aiming.BAD_SHOOTS_MAX = 0;
            settings.FileSettings.Aiming.BAD_SHOOTS_MIN = 0;
            settings.FileSettings.Aiming.BAD_SHOOTS_OFFSET = 0;

            settings.FileSettings.Look.MINIMUM_VISIBLE_DIST = 5f * botDifficultyInt;
            settings.FileSettings.Look.CAN_USE_LIGHT = true;
            settings.FileSettings.Look.NIGHT_VISION_ON = settings.FileSettings.Look.MINIMUM_VISIBLE_DIST;
            settings.FileSettings.Look.NIGHT_VISION_OFF = settings.FileSettings.Look.MINIMUM_VISIBLE_DIST;
            settings.FileSettings.Look.NIGHT_VISION_DIST = settings.FileSettings.Look.MINIMUM_VISIBLE_DIST;
            settings.FileSettings.Look.VISIBLE_ANG_NIGHTVISION = 360f;
            settings.FileSettings.Look.LOOK_THROUGH_PERIOD_BY_HIT = 5f;
            settings.FileSettings.Look.LightOnVisionDistance = settings.FileSettings.Look.MINIMUM_VISIBLE_DIST;
            settings.FileSettings.Look.LOOK_LAST_POSENEMY_IF_NO_DANGER_SEC = 25f;
            settings.FileSettings.Look.VISIBLE_ANG_LIGHT = 55f;
            settings.FileSettings.Look.VISIBLE_DISNACE_WITH_LIGHT = 80f;
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR = 1.5f;
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR_GREEN = 2f;
            settings.FileSettings.Look.LOOK_THROUGH_GRASS = false;
            settings.FileSettings.Look.DIST_REPEATED_SEEN = 50.0f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS = 0.01f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS_FLARE = 0.01f;
            settings.FileSettings.Look.NO_GREEN_DIST = 20.0f;
            settings.FileSettings.Look.NO_GRASS_DIST = 20.0f;
            settings.FileSettings.Look.CHECK_HEAD_ANY_DIST = true;
            settings.FileSettings.Look.MIDDLE_DIST_CAN_SHOOT_HEAD = true;

            settings.FileSettings.Hearing.CHANCE_TO_HEAR_SIMPLE_SOUND_0_1 = 1f;
            settings.FileSettings.Hearing.DISPERSION_COEF = 10f + 10f * botDifficultyInt;
            settings.FileSettings.Hearing.DISPERSION_COEF_GUN = 100f + 20f * botDifficultyInt;
            settings.FileSettings.Hearing.CLOSE_DIST = settings.FileSettings.Hearing.CLOSE_DIST + botDifficultyInt * 3f;
            settings.FileSettings.Hearing.FAR_DIST += settings.FileSettings.Hearing.CLOSE_DIST + botDifficultyInt * 2f;
            settings.FileSettings.Hearing.SOUND_DIR_DEEFREE *= botDifficultyInt;
            settings.FileSettings.Hearing.LOOK_ONLY_DANGER = true;
            settings.FileSettings.Hearing.HEAR_DELAY_WHEN_PEACE = 0.1f;
            settings.FileSettings.Hearing.HEAR_DELAY_WHEN_HAVE_SMT = 0.1f;
            settings.FileSettings.Hearing.RESET_TIMER_DIST = 5f;

            settings.FileSettings.Shoot.WAIT_NEXT_SINGLE_SHOT = 0f;
            settings.FileSettings.Shoot.WAIT_NEXT_SINGLE_SHOT_LONG_MAX = 2f - botDifficultyInt * 0.2f;
            settings.FileSettings.Shoot.NEXT_SINGLE_SHOT_PAUSE = 0f;
            settings.FileSettings.Shoot.SHOOT_IMMEDIATELY_DIST = 300f;

            settings.FileSettings.Grenade.NO_RUN_FROM_AI_GRENADES = false;

            botOwner.GetPlayer.ActiveHealthController.SetDamageCoeff(1f);

            botOwner.LookSensor.ShootFromEyes = true;
            settings.FileSettings.Look.SHOOT_FROM_EYES = true;

            botOwner.GetPlayer.Physical.Stamina.ForceMode = true;
            botOwner.GetPlayer.Physical.HandsStamina.ForceMode = true;
            botOwner.GetPlayer.HealthController.DisableMetabolism();
            botOwner.Tactic.AggressionChange(1f);
            return settings;
        }
    }
}
