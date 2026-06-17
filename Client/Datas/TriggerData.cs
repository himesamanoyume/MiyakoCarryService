
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class TriggerData : BaseData
    {
        public abstract Vector3 GetPos();
        public abstract string GetActionName();
        public abstract string GetActionTargetName(Vector3 myPlayerPos);
        public abstract bool IsDisabled();
    }
}