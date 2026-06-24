
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class InteractiveObjectData : GameWorldData, IProxyActor
    {
        public abstract string Id();
        
        public abstract bool IsProxyActionAllowed();

        public abstract void ExcuteProxyAction();
    }
}