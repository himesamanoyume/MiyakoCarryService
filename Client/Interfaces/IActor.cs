
using UnityEngine;

namespace MiyakoCarryService.Client.Interfaces
{
    public interface IActor
    {
        public abstract string GetActionName();
        public abstract string GetActionTargetName(Vector3 myPlayerPos);
        public abstract bool IsDisabled();
    }
}