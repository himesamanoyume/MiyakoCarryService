
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using MiyakoCarryService.Client.Bots.Brain.Layers;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class BrainMgr : BaseMgr<BrainMgr>
    {
        private List<string> _customBrainNames;
        public sealed override void Start()
        {
            base.Start();

            TasksExtensions.HandleExceptions(GetAllCustomBrainName());

            BrainManager.RemoveLayer("Exfiltration", [nameof(EBrainName.PmcUsec), nameof(EBrainName.PmcBear)]);

            BrainManager.AddCustomLayer(typeof(McsCommonLayer), Classification.AllBrainNames, 65);
            BrainManager.AddCustomLayer(typeof(McsEscortLayer), Classification.AllBrainNames, 66);
            BrainManager.AddCustomLayer(typeof(McsProxyLayer), Classification.AllBrainNames, 67);
            BrainManager.AddCustomLayer(typeof(McsAvoidDangerLayer), Classification.AllBrainNames, 68);
            BrainManager.AddCustomLayer(typeof(McsFightLayer), Classification.AllBrainNames.Except([nameof(EBrainName.BossZryachiy), nameof(EBrainName.SctPredvst)]).ToList(), 88);
            BrainManager.AddCustomLayer(typeof(McsExfiltrationLayer), Classification.AllBrainNames, 89);
            BrainManager.AddCustomLayer(typeof(McsFightLayer), [nameof(EBrainName.BossZryachiy), nameof(EBrainName.SctPredvst)], 186);
        }

        private async Task GetAllCustomBrainName()
        {
            _customBrainNames = await McsRequestHandler.GetAllCustomBrainName();
            if (_customBrainNames != null && _customBrainNames.Count > 0)
            {
                BrainManager.AddCustomLayer(typeof(McsCommonLayer), _customBrainNames, 65);
                BrainManager.AddCustomLayer(typeof(McsEscortLayer), _customBrainNames, 66);
                BrainManager.AddCustomLayer(typeof(McsProxyLayer), _customBrainNames, 67);
                BrainManager.AddCustomLayer(typeof(McsAvoidDangerLayer), _customBrainNames, 68);
                BrainManager.AddCustomLayer(typeof(McsFightLayer), _customBrainNames, 88);
                BrainManager.AddCustomLayer(typeof(McsExfiltrationLayer), _customBrainNames, 89);
            }
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            BrainManager.RestoreLayers(["Exfiltration"], [nameof(EBrainName.PmcUsec), nameof(EBrainName.PmcBear)]);

            BrainManager.RemoveLayer(nameof(McsCommonLayer), Classification.AllBrainNames);
            BrainManager.RemoveLayer(nameof(McsEscortLayer), Classification.AllBrainNames);
            BrainManager.RemoveLayer(nameof(McsProxyLayer), Classification.AllBrainNames);
            BrainManager.RemoveLayer(nameof(McsAvoidDangerLayer), Classification.AllBrainNames);
            BrainManager.RemoveLayer(nameof(McsFightLayer), Classification.AllBrainNames);
            BrainManager.RemoveLayer(nameof(McsExfiltrationLayer), Classification.AllBrainNames);

            if (_customBrainNames != null && _customBrainNames.Count > 0)
            {
                BrainManager.RemoveLayer(nameof(McsCommonLayer), _customBrainNames);
                BrainManager.RemoveLayer(nameof(McsEscortLayer), _customBrainNames);
                BrainManager.RemoveLayer(nameof(McsProxyLayer), _customBrainNames);
                BrainManager.RemoveLayer(nameof(McsAvoidDangerLayer), _customBrainNames);
                BrainManager.RemoveLayer(nameof(McsFightLayer), _customBrainNames);
                BrainManager.RemoveLayer(nameof(McsExfiltrationLayer), _customBrainNames);
            }
        }
    }
}