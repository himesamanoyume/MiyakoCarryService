
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class InteractiveObjectData : GameWorldData, IProxyActor
    {
        public abstract bool IsProxyActionAllowed();

        public abstract void ExcuteProxyAction();
    }
}