using EFT.Interactive;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class InteractableObjectData : WorldData, IProxyActor
    {
        public abstract string Id();
        public abstract bool IsProxyActionDisabled();
        public abstract WorldInteractiveObject GetWorldInteractiveObject();
    }
}