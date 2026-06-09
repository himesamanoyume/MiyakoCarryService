using System;
using EFT.Interactive;

namespace MiyakoCarryService.Client.Datas
{
    public class TransitData : BaseData
    {
        private WeakReference<TransitPoint> _transitRef;
        public TransitPoint TransitPoint => _transitRef.TryGetTarget(out var transit) ? transit : null;
        public bool HasUnmetRequirements = false;

        public TransitData(TransitPoint transitPoint) : base()
        {
            _transitRef = new WeakReference<TransitPoint>(transitPoint);
        }
    }
}