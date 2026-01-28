
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
            BrainManager.AddCustomLayer(typeof(McsCommonLayer), Classification.AllBrainNames, 75);
        }

        protected override void Refresh()
        {
            
        }
    }
}