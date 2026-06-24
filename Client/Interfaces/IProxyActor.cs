
namespace MiyakoCarryService.Client.Interfaces
{
    public interface IProxyActor
    {
        public abstract bool IsProxyActionDisabled();
        public abstract void ExcuteProxyAction();
    }
}