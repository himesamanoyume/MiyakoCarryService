
namespace MiyakoCarryService.Client.Interfaces
{
    public interface IProxyActor
    {
        public abstract bool IsProxyActionAllowed();
        public abstract void ExcuteProxyAction();
    }
}