
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Layers;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class BrainMgr : BaseMgr<BrainMgr>
    {
        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        public sealed override void Start()
        {
            base.Start();
            GameLoop.Instance.CheckVaildGameWorld();
            if (MiyakoCarryServicePlugin.IsLoadedByScriptEngine && GameLoop.Instance.IsVaildGameWorld)
            {
                GameLoop.Instance.IsGameStarted = true;
                EventMgr.Notify(new GameWorldStartedEvent
                {
                    GameWorld = Singleton<GameWorld>.Instance,
                });
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