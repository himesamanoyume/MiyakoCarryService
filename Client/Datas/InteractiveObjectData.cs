
using MiyakoCarryService.Client.Interfaces;
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class InteractiveObjectData : BaseData, IProxyActor, IActor
    {
        public abstract bool IsProxyActionAllowed();

        public abstract void ExcuteProxyAction();

        public abstract string GetActionName();

        public abstract string GetActionTargetName(Vector3 myPlayerPos);

        public abstract bool IsDisabled();
    }
}