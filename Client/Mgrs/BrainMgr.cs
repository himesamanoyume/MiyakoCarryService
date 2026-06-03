
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

            BrainManager.AddCustomLayer(typeof(McsExfiltrationLayer), Classification.AllBrainNames, 89);

            BrainManager.AddCustomLayer(typeof(McsCommonLayer), Classification.AllBrainNames, 65);

            if (MiyakoCarryServicePlugin.SAINInstalled)
            {
                BrainManager.AddCustomLayer(typeof(McsFightLayer), Classification.AllBrainNames.Except([nameof(EBrainName.BossZryachiy), nameof(EBrainName.SctPredvst)]).ToList(), 66);

                BrainManager.AddCustomLayer(typeof(McsFightLayer), [nameof(EBrainName.BossZryachiy), nameof(EBrainName.SctPredvst)], 186);
            }
            else
            {
                BrainManager.AddCustomLayer(typeof(McsFightLayer), Classification.AllBrainNames.Except([nameof(EBrainName.BossZryachiy), nameof(EBrainName.SctPredvst)]).ToList(), 86);

                BrainManager.AddCustomLayer(typeof(McsFightLayer), [nameof(EBrainName.BossZryachiy), nameof(EBrainName.SctPredvst)], 186);
            }
        }

        private async Task GetAllCustomBrainName()
        {
            _customBrainNames = await McsRequestHandler.GetAllCustomBrainName();
            if (_customBrainNames != null && _customBrainNames.Count > 0)
            {
                BrainManager.AddCustomLayer(typeof(McsCommonLayer), _customBrainNames, 65);
                
                BrainManager.AddCustomLayer(typeof(McsFightLayer), _customBrainNames, 86);
            }
        }
    }
}