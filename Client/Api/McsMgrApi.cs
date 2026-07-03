
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Api
{
    public static class McsMgrApi
    {
        public static T GetMgr<T>() where T : IMgr
        {
            return MgrAccessor.Get<T>();
        }
    }
}