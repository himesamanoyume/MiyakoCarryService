
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
            BrainManager.RemoveLayer("Exfiltration", [EBrainName.PmcUsec.ToString(), EBrainName.PmcBear.ToString()]);
            BrainManager.AddCustomLayer(typeof(McsExfiltrationLayer), [EBrainName.PmcUsec.ToString(), EBrainName.PmcBear.ToString()], 89);
            BrainManager.AddCustomLayer(typeof(McsCommonLayer), Classification.AllBrainNames, 65);
            if (!MiyakoCarryServicePlugin.SAINInstalled)
            {
                BrainManager.AddCustomLayer(typeof(McsFightLayer), Classification.AllBrainNames.Except(["BossZryachiy"]).ToList(), 86);
            }
        }
    }
}