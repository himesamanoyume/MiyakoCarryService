
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class WorldData : BaseData
    {
        public abstract string GetActionName();
        public abstract string GetActionTargetName(Vector3 myPlayerPos);
        public abstract bool IsDisabled();
        public abstract Vector3 GetPos();
    }
}