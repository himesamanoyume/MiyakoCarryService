
using DrakiaXYZ.BigBrain.Brains;
using MiyakoCarryService.Client.Bots.Brain.Layers;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class BrainMgr : BaseMgr<BrainMgr>
    {
        public sealed override void Start()
        {
            base.Start();
            BrainManager.AddCustomLayer(typeof(FollowMcsBossLayer), Classification.AllBrainNames, 100);
        }

        protected override void Reset()
        {
            
        }
    }
}