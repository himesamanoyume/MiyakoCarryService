
using DrakiaXYZ.BigBrain.Brains;
using MiyakoCarryService.Client.Bots.Brain.Layers;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class BrainMgr : BaseMgr<BrainMgr>
    {
        public sealed override void Start()
        {
            base.Start();
            _gameloop.Mgrs.Add(EMgrType.BRAIN, this);
            BrainManager.AddCustomLayer(typeof(FollowMcsBossLayer), Classification.AllBrainNames, 100);
        }

        protected override void Reset()
        {
            
        }
    }
}