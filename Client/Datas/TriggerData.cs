
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class TriggerData : BaseData
    {
        public abstract Transform GetTransfrom();
        public abstract string GetActionName();
        public abstract string GetActionTargetName(Vector3 myPlayerPos);
        public abstract bool IsDisabled();
    }
}