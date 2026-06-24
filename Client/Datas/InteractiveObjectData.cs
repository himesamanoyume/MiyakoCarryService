
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class InteractiveObjectData : WorldData, IProxyActor
    {
        public abstract string Id();
        
        public abstract bool IsProxyActionDisabled();

        public abstract void ExcuteProxyAction();
    }
}