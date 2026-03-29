
using System.Linq;
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
            BrainManager.RemoveLayer("Exfiltration", [nameof(EBrainName.PmcUsec), nameof(EBrainName.PmcBear)]);
            BrainManager.AddCustomLayer(typeof(McsExfiltrationLayer), [nameof(EBrainName.PmcUsec), nameof(EBrainName.PmcBear)], 89);
            BrainManager.AddCustomLayer(typeof(McsCommonLayer), Classification.AllBrainNames, 65);
            if (!MiyakoCarryServicePlugin.SAINInstalled)
            {
                BrainManager.AddCustomLayer(typeof(McsFightLayer), Classification.AllBrainNames.Except([nameof(EBrainName.BossZryachiy)]).Except(Classification.SAINNotAdjusted).ToList(), 86);
            }
            BrainManager.AddCustomLayer(typeof(McsFightLayer), Classification.SAINNotAdjusted.Except([nameof(EBrainName.SctPredvst)]).ToList(), 86);
            BrainManager.AddCustomLayer(typeof(McsFightLayer), [nameof(EBrainName.SctPredvst)], 186);
        }
    }
}