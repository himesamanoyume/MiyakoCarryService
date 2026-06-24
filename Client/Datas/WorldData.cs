
using MiyakoCarryService.Client.Interfaces;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class WorldData : BaseData, IActor
    {
        public abstract string GetActionName();
        public abstract string GetActionTargetName(Vector3 myPlayerPos);
        public abstract bool IsDisabled();
    }
}