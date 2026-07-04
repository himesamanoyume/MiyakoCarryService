
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Layers;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Misc;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class BrainMgr : BaseMgr<BrainMgr>
    {
        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        public sealed override void Start()
        {
            base.Start();
            GameLoop.Instance.IsGameStarted = Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld;
            GameLoop.Instance.CheckVaildGameWorld();
            if (MiyakoCarryServicePlugin.IsLoadedByScriptEngine && GameLoop.Instance.IsVaildGameWorld)
            {
                McsMgr.IsHost = true;
                EventMgr.Notify(new GameWorldStartedEvent
                {
                    GameWorld = Singleton<GameWorld>.Instance,
                });
                TasksExtensions.HandleExceptions(Reload());

            }
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            BrainUtils.OnRaidEnded();
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

            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                McsMgr.McsLeadPlayerConfigs = await McsRequestHandler.GetMcsBotPlayerConfigs();
            }
            else
            {
                McsMgr.McsLeadPlayerConfigs = new();
            }

            var leadPlayers = mcsProfilesDict
                .Where(kvp => kvp.Value.Length > 0)
                .Select(kvp => Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(kvp.Key))
                .Where(leadPlayer => leadPlayer != null);

            foreach (var leadPlayer in leadPlayers)
            {
                var mcsAILeadPlayer = new McsAILeadPlayer(leadPlayer);
                foreach (var mcsProfileItem in mcsProfilesDict)
                {
                    foreach (var mcsProfile in mcsProfileItem.Value)
                    {
                        McsMgr.AddMcsSquadMember(leadPlayer.ProfileId, mcsProfile.ProfileId, mcsAILeadPlayer);
                    }
                }
            }

            var mcsBotPlayers = McsMgr.GetAllMcsBotPlayer();

            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                var baseBrain = mcsBotPlayer?.BotOwner?.Brain?.BaseBrain;
                if (baseBrain == null)
                {
                    continue;
                }

                InjectLayers(baseBrain);
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

                BrainUtils.McsRestoreLayers(mcsBotPlayer.AIData.BotOwner, Classification.RemoveLayerNames);
                BrainUtils.McsRemoveLayer(mcsBotPlayer.AIData.BotOwner, nameof(McsCommonLayer));
                BrainUtils.McsRemoveLayer(mcsBotPlayer.AIData.BotOwner, nameof(McsEscortLayer));
                BrainUtils.McsRemoveLayer(mcsBotPlayer.AIData.BotOwner, nameof(McsAvoidDangerLayer));
                BrainUtils.McsRemoveLayer(mcsBotPlayer.AIData.BotOwner, nameof(McsProxyLayer));
                BrainUtils.McsRemoveLayer(mcsBotPlayer.AIData.BotOwner, nameof(McsFightLayer));
                BrainUtils.McsRemoveLayer(mcsBotPlayer.AIData.BotOwner, nameof(McsExfiltrationLayer));
            }
        }

        public void InjectLayers(BaseBrain baseBrain)
        {
            BrainUtils.McsRemoveLayers(baseBrain.Owner, Classification.RemoveLayerNames);
            BrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsCommonLayer), 65);
            BrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsEscortLayer), 66);
            BrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsAvoidDangerLayer), 67);
            BrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsProxyLayer), 68);
            BrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsFightLayer), baseBrain.ShortName() == nameof(EBrainName.BossZryachiy) || baseBrain.ShortName() == nameof(EBrainName.BossZryachiy) ? 186 : 88);
            BrainUtils.McsAddCustomLayer(baseBrain.Owner, typeof(McsExfiltrationLayer), 89);
        }
    }
}