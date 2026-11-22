using System.Linq;
using EFT.Interactive;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public class TransitData : TriggerData
    {
        private TransitPoint _transitRef;
        public TransitPoint TransitPoint => _transitRef;

        public TransitData(TransitPoint transitPoint) : base()
        {
            _transitRef = transitPoint;
            _colliders = transitPoint.transform.GetComponentsInChildren<Collider>().ToList();
        }

        public override string GetActionName() => TransitPoint.parameters.description.McsLocalized();

        public override string GetActionTargetName(Vector3 myPlayerPos) => string.Format(Locales.GETACTIONTARGETNAME_TARGETNAME.McsLocalized(), Mathf.RoundToInt(Vector3.Distance(myPlayerPos, TransitPoint.gameObject.transform.position)));
        
        public override bool IsDisabled() => !TransitPoint.IsActive;

        public override void Dispose()
        {
            base.Dispose();
            _transitRef = null;
        }
    }
}