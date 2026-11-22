
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Layers;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Extensions.ToIl2Cpp;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using System;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    internal class BrainMgr : BaseMgr
    {
        public BrainMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        private McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();
        
        protected override void OnMgrStart()
        {
            base.OnMgrStart();
            InitCustomLayerMaps();
            var gameWorld = Singleton<GameWorld>.Instance;
            Gameloop.IsGameStarted = Singleton<GameWorld>.Instantiated && gameWorld != null && gameWorld.TryCast<HideoutGameWorld>() == null;
            Gameloop.CheckVaildGameWorld();
            if (Gameloop.IsVaildGameWorld)
            {
                McsMgr.IsHost = true;
                EventMgr.Notify(new GameWorldStartedEvent
                {
                    GameWorld = Singleton<GameWorld>.Instance,
                });
                Reload().McsHandleExceptions();
            }
        }

        private void InitCustomLayerMaps()
        {
            LayerUtils.RegisterCustomLayer(typeof(McsBrainLayer), 200);
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            LayerUtils.OnRaidEnded();
        }

        private async Task Reload()
        {
            var myPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (myPlayer == null)
            {
                return;
            }

            var mcsProfilesDict = await McsRequestHandler.GetMcsBotPlayers(new()
            {
                Side = myPlayer.Side is EPlayerSide.Bear or EPlayerSide.Usec ? ESideType.Pmc : ESideType.Savage
            });

            if (mcsProfilesDict.Count == 0)
            {
                return;
            }

            await Gameloop.InitMcsLeadPlayerConfigs();

            var leadPlayers = mcsProfilesDict
                .Where(kvp => kvp.Value.Length > 0)
                .Select(kvp => Singleton<GameWorld>.Instance.GetEverExistedPlayerByProfileId(kvp.Key))
                .Where(leadPlayer => leadPlayer != null);

            foreach (var leadPlayer in leadPlayers)
            {
                if (leadPlayer.ProfileId == myPlayer.ProfileId)
                {
                    var mcsBotPlayerConfig = new McsBotPlayerConfig
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
                    McsMgr.UpdateMcsBotPlayerConfig(mcsBotPlayerConfig.McsLeadPlayerId, mcsBotPlayerConfig);
                }
                var mcsAILeadPlayer = new McsAILeadPlayer(leadPlayer);
                foreach (var mcsProfileItem in mcsProfilesDict)
                {
                    foreach (var mcsBotPlayerProfile in mcsProfileItem.Value)
                    {
                        var mcsBotPlayer = McsMgr.TryGetMcsBotPlayer(mcsBotPlayerProfile.ProfileId, leadPlayer.ProfileId);
                        var baseBrain = mcsBotPlayer?.BotOwner?.Brain?.BaseBrain;
                        if (baseBrain == null)
                        {
                            continue;
                        }
                        McsMgr.AddMcsSquadMember(leadPlayer.ProfileId, mcsBotPlayer.ProfileId, mcsAILeadPlayer);
                        var botOwner = mcsBotPlayer.BotOwner;
                        var wildSpawnType = mcsBotPlayer.Profile.Info.Settings.Role;
                        var botDifficulty = mcsBotPlayer.Profile.Info.Settings.BotDifficulty;
                        var settings = Gameloop.SetBotSettings(botDifficulty, wildSpawnType, botOwner, leadPlayer);
                        botOwner.Settings = settings;
                        InjectLayers(baseBrain);
                    }
                }
            }
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();

            var mcsBotPlayers = McsMgr.GetAllMcsBotPlayer();
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                if (mcsBotPlayer == null)
                {
                    continue;
                }

                LayerUtils.McsRestoreAllExcludedLayers(mcsBotPlayer.AIData.BotOwner);

                var customLayerMaps = LayerUtils.GetCustomLayerMaps();
                foreach ((var customLayerType, var priority) in customLayerMaps)
                {
                    LayerUtils.McsRemoveLayer(mcsBotPlayer.AIData.BotOwner, customLayerType.Name);
                }
            }
        }

        public void InjectLayers(BaseBrain baseBrain)
        {
            LayerUtils.McsRemoveNonKeepLayers(baseBrain._owner, Classification.KeepLayerNames);

            var customLayerMaps = LayerUtils.GetCustomLayerMaps();
            foreach ((var customLayerType, var priority) in customLayerMaps)
            {
                LayerUtils.McsAddCustomLayer(baseBrain._owner, customLayerType, priority);
            }
        }
    }
}