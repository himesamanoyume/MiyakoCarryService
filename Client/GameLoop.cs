using Comfort.Common;
using SPTushonka.Reflection.Utils;
using EFT;
using System;
using MiyakoCarryService.Client.Utils;
using System.Collections.Generic;
using MiyakoCarryService.Client.Mgrs;
using UnityEngine;
using System.IO;
using System.Reflection;
using MiyakoCarryService.Client.Bots.Navigation;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Patches.Events;
using System.Linq;
using MiyakoCarryService.Client.Models;
using System.Diagnostics;
using System.Threading;
using MiyakoCarryService.Client.Extensions;
using EFT.InventoryLogic;
using Diz.Jobs;
using MiyakoCarryService.Client.Extensions.ToIl2Cpp;
using System.Threading.Tasks;
using Il2CppSystem.Linq;
using Il2CppInterop.Runtime.Injection;
using SPTushonka.Reflection.Il2Cpp;

namespace MiyakoCarryService.Client
{
    public sealed class GameLoop : MonoBehaviour
    {
        public GameLoop(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public static GameLoop Instance => MiyakoCarryServiceSingleton<GameLoop>.Instance;

        public Dictionary<Type, BaseMgr> Mgrs { get; private set; } = new();
        public Dictionary<string, TraderOffer> ItemBestPriceDict { get; private set; } = new();
        public Shader HighlightShader { get; private set; } = null;
        public Camera MainCamera { get; private set; } = null;
        public Camera OpticCamera { get; private set; } = null;
        public Player MyPlayer { get; private set; } = null;
        public bool IsGameStarted = false;
        private Debouncer<ItemData, McsAILeadPlayer> _updateDebouncer;
        private HashSet<string> _loadedMcsLeadPlayer = new();
        private int? _lastSpawnGameWorldInstanceId = null;

        public IEftSession Session
        {
            get
            {
                return field ??= ClientAppUtils.GetClientApp().GetClientBackEndSession();
            }
        }

        public bool IsVaildGameWorld = false;

        public bool IsAiming
        {
            get
            {
                if (MyPlayer == null || MyPlayer.HandsController == null)
                {
                    return false;
                }

                return MyPlayer.HandsController.IsAiming;
            }
        }

        public bool CheckVaildGameWorld()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            IsVaildGameWorld = Singleton<GameWorld>.Instantiated && gameWorld != null && gameWorld.TryCast<HideoutGameWorld>() == null && IsGameStarted;
            return IsVaildGameWorld;
        }

        void Update()
        {
            CheckVaildGameWorld();

            KeyInput.KeyDown(MiyakoCarryServicePlugin.EnableLootingHotKey.Value, MiyakoCarryServicePlugin.EnableLooting);
            KeyInput.KeyDown(MiyakoCarryServicePlugin.EnableKeepFormationHotKey.Value, MiyakoCarryServicePlugin.EnableKeepFormation);

            CommandUtils.KeepCommandMenuOpen();

            if (!IsVaildGameWorld)
            {
                return;
            }

            if (MyPlayer == null)
            {
                var mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                if (mainPlayer != null)
                {
                    MyPlayer = mainPlayer;
                }
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

                var bundle = AssetBundle.LoadFromMemory(assetBytes);
                if (bundle != null)
                {
                    HighlightShader = bundle.LoadAsset<Shader>(highlightShaderName)?.KeepLoaded();
                    if (HighlightShader == null)
                    {
                        McsLogger.LogFatal(new Exception($"无法加载Shader: {highlightShaderName}"));
                    }

                    return;
                }
            }
        }

        public void Init()
        {
            LoadAssetBundle();

            Il2CppActivator.Enable(typeof(McsMgr));
            Il2CppActivator.Enable(typeof(PlayerDataMgr));
            Il2CppActivator.Enable(typeof(LootDataMgr));
            Il2CppActivator.Enable(typeof(SubtitlesMgr));
            Il2CppActivator.Enable(typeof(CommandMgr));
            Il2CppActivator.Enable(typeof(HighlightMgr));
            Il2CppActivator.Enable(typeof(ExfilDataMgr));
            Il2CppActivator.Enable(typeof(TransitDataMgr));
            Il2CppActivator.Enable(typeof(QuestDataMgr));
            Il2CppActivator.Enable(typeof(SwitchDataMgr));
            Il2CppActivator.Enable(typeof(TripwireDataMgr));
            Il2CppActivator.Enable(typeof(BarbedWireDataMgr));
            Il2CppActivator.Enable(typeof(BorderZoneDataMgr));
            Il2CppActivator.Enable(typeof(DamageTriggerDataMgr));
            Il2CppActivator.Enable(typeof(RoomTrapDataMgr));
            Il2CppActivator.Enable(typeof(DoorDataMgr));
            Il2CppActivator.Enable(typeof(StationaryWeaponDataMgr));
            Il2CppActivator.Enable(typeof(FormationDataMgr));
            Il2CppActivator.Enable(typeof(TrapAvoidanceMgr));
            Il2CppActivator.Enable(typeof(BrainMgr));

            EventMgr.Subscribe<GameWorldStartedEvent>(OnGameWorldStarted, this);
            EventMgr.Subscribe<GameWorldEndedEvent>(OnGameWorldEnded, this);
            EventMgr.Subscribe<UpdateProfileEvent>(OnUpdateProfile, this);
            EventMgr.Subscribe<UpdateDailyQuestsEvent>(OnUpdateDailyQuests, this);
            EventMgr.Subscribe<UpdateMiyakoTraderAssortmentEvent>(OnUpdateMiyakoTraderAssortment, this);
            EventMgr.Subscribe<GameLoopMgrEnableEvent>(OnMgrEnable, this);
        }

        public void OnMgrEnable(GameLoopMgrEnableEvent @event)
        {
            foreach (var mgrType in @event.MgrTypes)
            {
                Il2CppActivator.Enable(mgrType);
            }
        }

        public void OnUpdateProfile(UpdateProfileEvent @event)
        {
            UpdateProfile().McsHandleExceptions();
        }

        public void OnUpdateDailyQuests(UpdateDailyQuestsEvent @event)
        {
            StartCoroutine(UpdateDailyQuests().McsSafe());
        }

        public void OnUpdateMiyakoTraderAssortment(UpdateMiyakoTraderAssortmentEvent @event)
        {
            UpdateMiyakoTraderAssortmentEvent().McsHandleExceptions();
        }

        public System.Collections.IEnumerator UpdateDailyQuests()
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(1f);

                TarkovApplication.Exist(out var tarkovApplication);
                if (tarkovApplication == null)
                {
                    continue;
                }

                var menuOperation = tarkovApplication._menuOperation;
                if (menuOperation?.QuestController?.Quests == null)
                {
                    yield break;
                }

                var repeatableQuestUpdater = menuOperation.QuestController.Quests._repeatableQuestUpdater;
                if (repeatableQuestUpdater == null)
                {
                    McsLogger.LogError("[UpdateDailyQuests] _repeatableQuestUpdater 为 null，跳过本次刷新");
                    yield break;
                }

                TasksExtensions.HandleExceptions(repeatableQuestUpdater.UpdateDailyQuests());
            }
        }

        public Task UpdateMiyakoTraderAssortmentEvent()
        {
            foreach (var trader in Session.Traders)
            {
                if (trader.Id == MiyakoCarryServicePlugin.MiyakoTraderId)
                {
                    TasksExtensions.HandleExceptions(trader.RefreshAssortment(true, true));
                    break;
                }
            }

            return Task.CompletedTask;
        }

        public async Task UpdateProfile()
        {
            var profileChangesPayload = await McsRequestHandler.UpdateProfile();
            var sessionBackendClass = Session == null ? null : Session.TryCast<EftClientBackendSession>();
            if (sessionBackendClass != null)
            {
                var profile = sessionBackendClass.Profile;
                var correctInfo = profile.Info;

                var traderInfos = Il2CppSystem.Linq.Enumerable.ToArray(profile.TradersInfo.Values);
                foreach (var traderInfo in traderInfos)
                {
                    if (traderInfo._profileInfo != correctInfo)
                    {
                        traderInfo._profileInfo = correctInfo;
                    }
                }

                var profileUpdaterFound = sessionBackendClass._profileUpdaters.TryGetValue(sessionBackendClass.Profile.Id, out var profileUpdater);
                if (profileUpdaterFound && profileUpdater != null && profileChangesPayload != null)
                {
                    var profileUpdatesHandler = profileUpdater.TryCast<ProfileUpdatesHandler>();
                    if (profileUpdatesHandler != null)
                    {
                        if (profileChangesPayload.Experience > profile.Info.Experience)
                        {
                            profileUpdatesHandler.ApplyExperience(profileChangesPayload.Experience);
                        }

                        if (profileChangesPayload.TradersData != null && profileChangesPayload.TradersData.Count > 0)
                        {
                            var tradersData = new Il2CppSystem.Collections.Generic.Dictionary<string, TraderData>();
                            foreach (var (traderId, traderData) in profileChangesPayload.TradersData)
                            {
                                var il2CppTraderData = new TraderData();
                                if (traderData.SalesSum.HasValue)
                                {
                                    il2CppTraderData._salesSum = new Il2CppSystem.Nullable<long>(traderData.SalesSum.Value);
                                }
                                if (traderData.Standing.HasValue)
                                {
                                    il2CppTraderData._standing = new Il2CppSystem.Nullable<double>(traderData.Standing.Value);
                                }
                                if (traderData.LoyaltyLevel.HasValue)
                                {
                                    il2CppTraderData._loyaltyLevel = new Il2CppSystem.Nullable<float>(traderData.LoyaltyLevel.Value);
                                }
                                if (traderData.Unlocked.HasValue)
                                {
                                    il2CppTraderData._unlocked = new Il2CppSystem.Nullable<bool>(traderData.Unlocked.Value);
                                }
                                if (traderData.Disabled.HasValue)
                                {
                                    il2CppTraderData._disabled = new Il2CppSystem.Nullable<bool>(traderData.Disabled.Value);
                                }

                                tradersData.Add(traderId, il2CppTraderData);
                            }

                            profileUpdatesHandler.ApplyTradersChanges(tradersData);
                        }
                    }
                }
            }
        }

        public void OnGameWorldStarted(GameWorldStartedEvent @event)
        {
            Reset();
            ClearInteropCaches();
        }

        public void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            Reset();
            ClearInteropCaches();
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

        public void Reset()
        {
            MainCamera = null;
            OpticCamera = null;
            MyPlayer = null;
        }

        public void Destroy()
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
                // BrainMgr 先进行，否则当McsMgr释放后无法获取到McsBotPlayers
                var brainMgr = MgrAccessor.Get<BrainMgr>();
                brainMgr.OnMgrDestroy();
                Mgrs.Remove(typeof(BrainMgr));
                // end
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
            ClearInteropCaches();
            MiyakoCarryServiceSingleton<GameLoop>.Clear();
            Destroy(gameObject);
        }

        private static void ClearInteropCaches()
        {
            Extensions.ItemExtensions.Clear();
            PlayerExtensions.Clear();
            BotOwnerExtensions.Clear();
            ConditionExtensions.Clear();
            DoorExtensions.Clear();
            SwitchExtensions.Clear();
            ExfiltrationPointExtensions.Clear();
            FinalExitZoneExtensions.Clear();
            TransitPointExtensions.Clear();
            TripwireSynchronizableObjectExtensions.Clear();
            TriggerZoneExtensions.Clear();
            TrapSyncableExtensions.Clear();
            StationaryWeaponExtensions.Clear();
            BorderZoneExtensions.Clear();
            DamageTriggerExtensions.Clear();
            NavGapExecutor.Clear();
        }

        internal T GetMgr<T>() where T : BaseMgr
        {
            return (T)Mgrs[typeof(T)];
        }

        internal HashSet<T> GetDatas<T, K>() where T : BaseData where K : DataMgr
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
                    currentBatch = new();
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
                        StartCoroutine(kvp.Key.UnlockRefreshRootItemInteresting(kvp.Value).McsSafe());
                    }
                    catch (Exception e)
                    {
                        McsLogger.LogError($"Batch refresh item error: {e}");
                    }
                }
            }
        }

        public async Task InitMcsLeadPlayerConfigs()
        {
            var mcsMgr = MgrAccessor.Get<McsMgr>();
            mcsMgr.McsLeadPlayerConfigs = new();
        }

        public async Task SpawnMcsBotPlayer()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var currentGameWorldInstanceId = gameWorld.GetInstanceID();
            if (_lastSpawnGameWorldInstanceId != currentGameWorldInstanceId)
            {
                _loadedMcsLeadPlayer.Clear();
                _lastSpawnGameWorldInstanceId = currentGameWorldInstanceId;
            }

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

            await InitMcsLeadPlayerConfigs();

            foreach (var mcsProfileItem in mcsProfilesDict)
            {
                foreach (var mcsProfile in mcsProfileItem.Value)
                {
                    var objectsFactory = Singleton<ObjectsFactory>.Instance;
                    if (objectsFactory == null)
                    {
                        return;
                    }

                    var pools = objectsFactory.GetPools(ObjectsFactory.PoolsCategory.Raid);
                    if (pools == null)
                    {
                        return;
                    }

                    await Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(
                        pools,
                        pools.ConvertResourceInfo(mcsProfile.GetAllPrefabPaths(false).ToList()),
                        ObjectsFactory.AssemblyType.Local,
                        JobYieldPriority.General,
                        new Il2CppSystem.Progress<InitLevelProgress>(),
                        new CancellationToken().ToIl2Cpp()).AsManaged();
                }
            }

            var leadPlayers = mcsProfilesDict
                .Where(kvp => kvp.Value.Length > 0)
                .Select(kvp => gameWorld.GetEverExistedPlayerByProfileId(kvp.Key))
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

            var botCreator = botSpawner._botCreator;
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
                        BlockItemType = (int)MiyakoCarryServicePlugin.BlockItemType.Value,
                        EnableKeepFormation = MiyakoCarryServicePlugin.EnableKeepFormation.Value,
                        FormationMatrix = MiyakoCarryServicePlugin.FormationMatrix.Value,
                        FormationSpacing = MiyakoCarryServicePlugin.FormationSpacing.Value,
                        FormationSequentialFill = MiyakoCarryServicePlugin.FormationSequentialFill.Value,
                        PhrasesSilent = MiyakoCarryServicePlugin.PhrasesSilent.Value,
                        EnableSubtitles = MiyakoCarryServicePlugin.EnableSubtitles.Value,
                    };
                    mcsMgr.UpdateMcsBotPlayerConfig(mcsBotPlayerConfig.McsLeadPlayerId, mcsBotPlayerConfig);
                }
                var mcsAILeadPlayer = new McsAILeadPlayer(leadPlayer);
                leadPlayer.Profile.Info.GroupId = "Mcs";

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
                        ShallBeGroup = new ShallBeGroupParams(true, false, 5),
                        Id_spawn = "Mcs"
                    };

                    var botProfileDataClass = new GetProfileDataParams(leadPlayer.Side, wildSpawnType, botDifficulty, 2, botSpawnParams);

                    var botCreationDataClass = await BotCreationData.Create(botProfileDataClass, botCreator, 0, botSpawner).AsManaged();

                    botCreationDataClass.AddProfile(mcsBotPlayerProfile);

                    var closestGroupPoint = botsController.CoversData.GetClosest(leadPlayerPos);
                    botCreationDataClass.AddPosition(leadPlayerPos, closestGroupPoint.CorePointInGame.Id);

                    var closestZone = botSpawner.GetClosestZone(leadPlayerPos, out _);

                    var groupAction = new Func<BotOwner, BotZone, BotSpawnBossAndGroupParams, BotsGroup>((BotOwner botOwner, BotZone botZone, BotSpawnBossAndGroupParams groupParams) =>
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

                            Tools.MakeNeutralBotTypes(leadPlayer.BotsGroup._initialBot?.Settings);
                            return leadPlayer.BotsGroup;
                        }

                        var enemyTypes = botOwner.Settings.GetEnemyBotTypes();

                        foreach (var neutralType in Classification.NeutralBotTypes)
                        {
                            enemyTypes.Remove(neutralType);
                        }

                        if (leadPlayer.Side != EPlayerSide.Savage)
                        {
                            foreach (WildSpawnType wst in Enum.GetValues(typeof(WildSpawnType)))
                            {
                                if (Tools.IsNeutralBot(wst))
                                {
                                    continue;
                                }

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

                        var botsGroup = new BotsGroup(closestZone, botGame, botOwner, botSpawner._deadBodiesController, true);

                        var notScav = leadPlayer.Side != EPlayerSide.Savage;
                        if (notScav)
                        {
                            botsGroup.add_OnReportEnemy(new Action<IPlayer, Vector3, Vector3, EEnemyPartVisibleType, BotOwner>((IPlayer enemy, Vector3 enemyPos, Vector3 weaponRootLast, EEnemyPartVisibleType isVisibleOnlyBySense, BotOwner reporter) =>
                            {
                                if (enemy.Profile.Info.GroupId is "Mcs" || mcsMgr.IsMcsLeadPlayer(enemy.ProfileId) || mcsMgr.IsMcsBotPlayer(enemy.ProfileId))
                                {
                                    return;
                                }

                                if (enemy.Profile?.Info?.Settings != null && Tools.IsNeutralBot(enemy.Profile.Info.Settings.Role))
                                {
                                    return;
                                }

                                if (!Tools.TryMakeRoomForEnemy(botsGroup))
                                {
                                    return;
                                }

                                botsGroup.AddEnemy(enemy);
                            }));
                        }

                        foreach (var _leadPlayer in leadPlayers)
                        {
                            botsGroup.BotGroupHostility.RemoveEnemyAction(_leadPlayer);
                            botsGroup.BotGroupHostility.AddNeutralAction(_leadPlayer);
                        }

                        botSpawner.Groups.AddNoKey(botsGroup, botZone);
                        botsGroup.AddMember(botOwner, false);

                        leadPlayer.BotsGroup = botsGroup;
                        leadPlayer.BotsGroup.Lock();

                        Tools.MakeNeutralBotTypes(botsGroup._initialBot?.Settings);

                        return botsGroup;
                    });

                    botCreationDataClass.Profiles.ForEach(new Action<Profile>(async profile =>
                    {
                        var onActivate = new Action<BotOwner>((BotOwner botOwner) =>
                        {
                            var stopWatch = new Stopwatch().ToIl2Cpp();
                            stopWatch.Start();

                            botSpawner.ActivateBotCallback(botOwner, botCreationDataClass, null, botCreationDataClass.SpawnParams.ShallBeGroup != null, stopWatch);

                            botOwner.Memory.DeleteInfoAboutEnemy(leadPlayer);

                            botOwner.GetPlayer.Physical.Stamina.ForceMode = true;
                            botOwner.GetPlayer.Physical.HandsStamina.ForceMode = true;

                            botOwner.GetPlayer.Profile.Info.GroupId = leadPlayer.Profile.Info.GroupId;
                            botOwner.GetPlayer.Profile.Info.TeamId = leadPlayer.Profile.Info.TeamId;

                            botOwner.BotFollower.PatrolDataFollower.InitPlayer(leadPlayer);
                            var pointChooser = PatrollingData.GetPointChooser(botOwner, PatrolMode.bossRoundProtectWithStay, botOwner.SpawnProfileData);
                            botOwner.PatrollingData.SetMode(PatrolMode.follower, pointChooser);
                            botOwner.BotFollower.BossToFollow = mcsAILeadPlayer;

                            var brainMgr = MgrAccessor.Get<BrainMgr>();
                            if (botOwner.Brain.BaseBrain != null)
                            {
                                brainMgr.InjectLayers(botOwner.Brain.BaseBrain);
                            }
                            else
                            {
                                Action<BaseBrain> handler = null;
                                handler = (BaseBrain b) =>
                                {
                                    botOwner.Brain.OnSetStrategy -= handler;
                                    brainMgr.InjectLayers(b);
                                };
                                botOwner.Brain.OnSetStrategy += handler;
                            }

                            EventMgr.Notify(new McsBotPlayerActivatedEvent
                            {
                                McsBotPlayerId = botOwner.ProfileId
                            });

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

                        botSpawner._inSpawnProcess += 1;

                        var cancellationToken = new CancellationToken().ToIl2Cpp();
                        await botCreator.ActivateBot(profile, new PositionNote(leadPlayerPos, botCreationDataClass.GetPosition().CorePointId, true), closestZone, new BotSpawnBossAndGroupParams(), groupAction, onActivate, cancellationToken);
                    }));
                }
            }
        }

        public BotSettings SetBotSettings(BotDifficulty botDifficulty, WildSpawnType wildSpawnType, BotOwner botOwner, Player leadPlayer)
        {
            var settings = Singleton<BotSettingsController>.Instance.GetSettings(botDifficulty, wildSpawnType, false);
            var carryServiceLevel = BotSettingUtils.GetCarryServiceLevel(botOwner.GetPlayer.Profile.Info.Level);

            ApplyMcsBotFileSettings(settings, botOwner, leadPlayer, carryServiceLevel);

            return settings;
        }

        public void ApplyMcsBotFileSettings(BotSettings settings, BotOwner botOwner, Player leadPlayer, int carryServiceLevel)
        {
            var notScav = leadPlayer.Side != EPlayerSide.Savage;

            settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = true;
            settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = notScav;

            settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = notScav ? EWarnBehaviour.AlwaysEnemies : EWarnBehaviour.Neutral;
            settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
            settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;

            settings.FileSettings.Mind.DIST_TO_STOP_RUN_ENEMY = 15f;
            settings.FileSettings.Mind.CHANCE_TO_RUN_CAUSE_DAMAGE_0_100 = 0f;

            settings.FileSettings.Mind.CAN_TALK = true;
            settings.FileSettings.Mind.TALK_WITH_QUERY = true;
            botOwner.BotTalk._canSay = true;

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
            settings.FileSettings.Mind.REVENGE_TO_GROUP = false;
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
            settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = Array.Empty<WildSpawnType>().ToIl2CppStructArray();
            settings.FileSettings.Mind.REVENGE_BOT_TYPES = Array.Empty<WildSpawnType>().ToIl2CppStructArray();

            Tools.MakeNeutralBotTypes(settings);
            Tools.MakeNeutralBotTypes(botOwner.Settings);

            settings.FileSettings.Mind.DOG_FIGHT_IN = 0f;
            settings.FileSettings.Mind.DOG_FIGHT_OUT = 0f;
            settings.FileSettings.Mind.SHOOT_INSTEAD_DOG_FIGHT = 0f;
            settings.FileSettings.Mind.MIN_DAMAGE_SCARE = 99999f;
            settings.FileSettings.Mind.DANGER_EXPIRE_TIME_MIN = 0f;
            settings.FileSettings.Mind.DANGER_EXPIRE_TIME_MAX = 0f;
            settings.FileSettings.Mind.PANIC_SIT_WEIGHT = 0f;
            settings.FileSettings.Mind.PANIC_LAY_WEIGHT = 0f;
            settings.FileSettings.Mind.PANIC_SIT_WEIGHT_PEACE = 0f;
            settings.FileSettings.Mind.PANIC_RUN_WEIGHT = 1f;
            settings.FileSettings.Mind.PANIC_NONE_WEIGHT = 1f;
            settings.FileSettings.Mind.CAN_PANIC_IS_PROTECT = false;
            settings.FileSettings.Mind.AVOID_BTR_RADIUS_SQR = 1f;
            settings.FileSettings.Mind.GRENADE_DAMAGE_IGNORE = false;
            settings.FileSettings.Mind.IGNORE_DANGER_PLACES = false;

            settings.FileSettings.Mind.PART_PERCENT_TO_HEAL = BotSettingUtils.GetBoostScaled(0.98f, carryServiceLevel);
            settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = BotSettingUtils.GetBoostScaled(30f, carryServiceLevel);
            settings.FileSettings.Mind.TIME_TO_FIND_ENEMY = BotSettingUtils.GetBoostScaled(20f, carryServiceLevel);
            settings.FileSettings.Mind.ATTACK_IMMEDIATLY_CHANCE_0_100 = BotSettingUtils.GetBoostScaled(48f, 100f, carryServiceLevel);
            settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG = BotSettingUtils.GetBoostScaled(180f, carryServiceLevel);
            settings.FileSettings.Mind.BULLET_FEEL_CLOSE_SDIST = BotSettingUtils.GetBoostScaled(30f, carryServiceLevel);
            settings.FileSettings.Mind.DIST_TO_ENEMY_SPOTTED_ON_HIT = BotSettingUtils.GetBoostScaled(200f, carryServiceLevel);

            settings.FileSettings.Move.REACH_DIST = 1.5f;
            settings.FileSettings.Move.REACH_DIST_COVER = 2f;
            settings.FileSettings.Move.REACH_DIST_RUN = 1.5f;
            settings.FileSettings.Move.DIST_SPRINT_GO_TO_SOME_POINT = 2f;
            settings.FileSettings.Move.WAIT_DOOR_OPEN_SEC = 1f;
            settings.FileSettings.Move.BREACH_CHANCE_100 = 0;

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

            settings.FileSettings.Core.CanGrenade = true;
            settings.FileSettings.Core.CanRun = true;
            settings.FileSettings.Core.VisibleDistance = BotSettingUtils.GetBoostScaled(480f, carryServiceLevel);
            botOwner.GetPlayer.ActiveHealthController.SetDamageCoeff(1f);

            settings.FileSettings.Aiming.MAX_AIMING_UPGRADE_BY_TIME = BotSettingUtils.GetWeakenScaled(0.25f, carryServiceLevel);
            settings.FileSettings.Aiming.BOTTOM_COEF = BotSettingUtils.GetWeakenScaled(0.25f, carryServiceLevel);
            settings.FileSettings.Aiming.COEF_FROM_COVER = BotSettingUtils.GetWeakenScaled(0.25f, carryServiceLevel);
            settings.FileSettings.Aiming.COEF_IF_MOVE = BotSettingUtils.GetWeakenScaled(0.25f, carryServiceLevel);
            settings.FileSettings.Aiming.TIME_COEF_IF_MOVE = BotSettingUtils.GetWeakenScaled(1f, carryServiceLevel);
            settings.FileSettings.Aiming.MISS_DIST = BotSettingUtils.GetBoostScaled(500f, carryServiceLevel);

            settings.FileSettings.Aiming.MAX_AIM_PRECICING = BotSettingUtils.GetBoostScaled(36f, 60f, carryServiceLevel);
            settings.FileSettings.Aiming.MAX_AIM_TIME = BotSettingUtils.GetWeakenScaled(0.458f, 0.05f, carryServiceLevel);
            settings.FileSettings.Aiming.HARD_AIM = BotSettingUtils.GetWeakenScaled(0.292f, 0.05f, carryServiceLevel);
            settings.FileSettings.Aiming.SCATTERING_HAVE_DAMAGE_COEF = BotSettingUtils.GetWeakenScaled(0.417f, 0f, carryServiceLevel);
            settings.FileSettings.Aiming.XZ_COEF = BotSettingUtils.GetWeakenScaled(0.375f, 0f, carryServiceLevel);
            settings.FileSettings.Aiming.SCATTERING_DIST_MODIF = BotSettingUtils.GetWeakenScaled(0.458f, 0.10f, carryServiceLevel);
            settings.FileSettings.Aiming.SCATTERING_DIST_MODIF_CLOSE = BotSettingUtils.GetWeakenScaled(0.458f, 0.10f, carryServiceLevel);
            settings.FileSettings.Aiming.BASE_SHIEF = BotSettingUtils.GetWeakenScaled(0.333f, 0.10f, carryServiceLevel);
            settings.FileSettings.Aiming.MISS_FIRST_SOOTS = BotSettingUtils.GetWeakenScaledInt(2f, carryServiceLevel);
            settings.FileSettings.Aiming.NEXT_SHOT_MISS_CHANCE_100 = BotSettingUtils.GetWeakenScaled(33.33f, 0f, carryServiceLevel);
            settings.FileSettings.Aiming.NEXT_SHOT_MISS_Y_OFFSET = BotSettingUtils.GetWeakenScaled(1.333f, 1.0f, carryServiceLevel);

            settings.FileSettings.Aiming.FIRST_CONTACT_ADD_SEC = BotSettingUtils.GetWeakenScaled(1.0f, 0f, carryServiceLevel);
            settings.FileSettings.Aiming.FIRST_CONTACT_ADD_CHANCE_100 = BotSettingUtils.GetWeakenScaled(50f, 0f, carryServiceLevel);
            settings.FileSettings.Aiming.ANY_PART_SHOOT_TIME = BotSettingUtils.GetWeakenScaled(8.333f, 5f, carryServiceLevel);

            settings.FileSettings.Aiming.HARD_AIM_CHANCE_100 = 100;
            settings.FileSettings.Aiming.PANIC_TIME = 0f;
            settings.FileSettings.Aiming.DAMAGE_PANIC_TIME = 0f;
            settings.FileSettings.Aiming.PANIC_COEF = 1f;
            settings.FileSettings.Aiming.PANIC_ACCURATY_COEF = 1f;
            settings.FileSettings.Aiming.DAMAGE_TO_DISCARD_AIM_0_100 = 0f;
            settings.FileSettings.Aiming.MIN_TIME_DISCARD_AIM_SEC = 0f;
            settings.FileSettings.Aiming.MAX_TIME_DISCARD_AIM_SEC = 0f;
            settings.FileSettings.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0f;
            settings.FileSettings.Aiming.BASE_HIT_AFFECTION_MIN_ANG = 0f;
            settings.FileSettings.Aiming.BASE_HIT_AFFECTION_MAX_ANG = 0f;
            settings.FileSettings.Aiming.BOT_MOVE_IF_DELTA = 0.01f;
            settings.FileSettings.Aiming.AIMING_TYPE = 6;
            settings.FileSettings.Aiming.DIST_TO_SHOOT_TO_CENTER = 0f;
            settings.FileSettings.Aiming.DIST_TO_SHOOT_NO_OFFSET = 0f;
            settings.FileSettings.Aiming.SHOOT_TO_CHANGE_PRIORITY = 5525;
            settings.FileSettings.Aiming.MISS_ON_START = 0;
            settings.FileSettings.Aiming.SHPERE_FRIENDY_FIRE_SIZE = 0.5f;
            settings.FileSettings.Aiming.WEAPON_ROOT_OFFSET = 0.35f;
            settings.FileSettings.Aiming.DANGER_UP_POINT = 0.1f;
            settings.FileSettings.Aiming.OFFSET_RECAL_ANYWAY_TIME = 1f;
            settings.FileSettings.Aiming.ANYTIME_LIGHT_WHEN_AIM_100 = 100f;
            settings.FileSettings.Aiming.BAD_SHOOTS_MAX = 0;
            settings.FileSettings.Aiming.BAD_SHOOTS_MIN = 0;
            settings.FileSettings.Aiming.BAD_SHOOTS_OFFSET = 0;

            settings.FileSettings.Look.MINIMUM_VISIBLE_DIST = BotSettingUtils.GetBoostScaled(480f, carryServiceLevel);
            settings.FileSettings.Look.NIGHT_VISION_ON = settings.FileSettings.Look.MINIMUM_VISIBLE_DIST;
            settings.FileSettings.Look.NIGHT_VISION_OFF = settings.FileSettings.Look.MINIMUM_VISIBLE_DIST;
            settings.FileSettings.Look.NIGHT_VISION_DIST = settings.FileSettings.Look.MINIMUM_VISIBLE_DIST;
            settings.FileSettings.Look.VISIBLE_ANG_NIGHTVISION = BotSettingUtils.GetBoostScaled(180f, carryServiceLevel);
            settings.FileSettings.Look.LOOK_THROUGH_PERIOD_BY_HIT = BotSettingUtils.GetBoostScaled(5f, carryServiceLevel);
            settings.FileSettings.Look.LightOnVisionDistance = BotSettingUtils.GetBoostScaled(5f, carryServiceLevel);
            settings.FileSettings.Look.LOOK_LAST_POSENEMY_IF_NO_DANGER_SEC = BotSettingUtils.GetBoostScaled(25f, carryServiceLevel);
            settings.FileSettings.Look.VISIBLE_ANG_LIGHT = BotSettingUtils.GetBoostScaled(180f, carryServiceLevel);
            settings.FileSettings.Look.VISIBLE_DISNACE_WITH_LIGHT = BotSettingUtils.GetBoostScaled(100f, carryServiceLevel);
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR = BotSettingUtils.GetBoostScaled(1.5f, carryServiceLevel);
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR_GREEN = BotSettingUtils.GetBoostScaled(2f, carryServiceLevel);
            settings.FileSettings.Look.DIST_REPEATED_SEEN = BotSettingUtils.GetBoostScaled(50f, carryServiceLevel);
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS = BotSettingUtils.GetBoostScaled(3f, carryServiceLevel);
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS_FLARE = BotSettingUtils.GetBoostScaled(3f, carryServiceLevel);
            settings.FileSettings.Look.NO_GREEN_DIST = BotSettingUtils.GetBoostScaled(20f, carryServiceLevel);
            settings.FileSettings.Look.NO_GRASS_DIST = BotSettingUtils.GetBoostScaled(20f, carryServiceLevel);
            settings.FileSettings.Look.FAR_DISTANCE = BotSettingUtils.GetBoostScaled(500f, carryServiceLevel);
            settings.FileSettings.Look.MIDDLE_DIST = BotSettingUtils.GetBoostScaled(300f, carryServiceLevel);
            settings.FileSettings.Look.MiddleDeltaTimeSec = BotSettingUtils.GetWeakenScaled(0.1f, carryServiceLevel);
            settings.FileSettings.Look.FarDeltaTimeSec = BotSettingUtils.GetWeakenScaled(0.1f, carryServiceLevel);

            settings.FileSettings.Look.CAN_USE_LIGHT = false;
            settings.FileSettings.Look.FULL_SECTOR_VIEW = true;
            settings.FileSettings.Look.LOOK_THROUGH_GRASS = false;
            settings.FileSettings.Look.CHECK_HEAD_ANY_DIST = true;
            settings.FileSettings.Look.MIDDLE_DIST_CAN_SHOOT_HEAD = true;
            botOwner.LookSensor.ShootFromEyes = false;
            settings.FileSettings.Look.SHOOT_FROM_EYES = false;

            settings.FileSettings.Hearing.CHANCE_TO_HEAR_SIMPLE_SOUND_0_1 = BotSettingUtils.GetBoostScaled(1f, carryServiceLevel);
            settings.FileSettings.Hearing.DISPERSION_COEF = BotSettingUtils.GetBoostScaled(80f, carryServiceLevel);
            settings.FileSettings.Hearing.DISPERSION_COEF_GUN = BotSettingUtils.GetBoostScaled(180f, carryServiceLevel);
            settings.FileSettings.Hearing.CLOSE_DIST = BotSettingUtils.GetBoostScaled(42f, carryServiceLevel);
            settings.FileSettings.Hearing.FAR_DIST = BotSettingUtils.GetBoostScaled(settings.FileSettings.Hearing.FAR_DIST + 68f, carryServiceLevel);
            settings.FileSettings.Hearing.SOUND_DIR_DEEFREE = BotSettingUtils.GetBoostScaled(settings.FileSettings.Hearing.SOUND_DIR_DEEFREE * 4f, carryServiceLevel);
            settings.FileSettings.Hearing.HEAR_DELAY_WHEN_PEACE = BotSettingUtils.GetWeakenScaled(0.01f, carryServiceLevel);
            settings.FileSettings.Hearing.HEAR_DELAY_WHEN_HAVE_SMT = BotSettingUtils.GetWeakenScaled(0.01f, carryServiceLevel);
            settings.FileSettings.Hearing.LOOK_ONLY_DANGER = true;
            settings.FileSettings.Hearing.RESET_TIMER_DIST = 5f;

            settings.FileSettings.Shoot.SHOOT_IMMEDIATELY_DIST = BotSettingUtils.GetBoostScaled(300f, carryServiceLevel);
            settings.FileSettings.Shoot.WAIT_NEXT_SINGLE_SHOT = 0f;
            settings.FileSettings.Shoot.WAIT_NEXT_SINGLE_SHOT_LONG_MAX = 1.2f;
            settings.FileSettings.Shoot.NEXT_SINGLE_SHOT_PAUSE = 0f;
            settings.FileSettings.Shoot.AGS_17_DIST_TO_LEAVE = 20f;

            settings.FileSettings.Grenade.NO_RUN_FROM_AI_GRENADES = false;

            botOwner.GetPlayer.Physical.Stamina.ForceMode = true;
            botOwner.GetPlayer.Physical.HandsStamina.ForceMode = true;
            botOwner.GetPlayer.HealthController.DisableMetabolism();
            botOwner.Tactic.AggressionChange(1f);
        }
    }
}
