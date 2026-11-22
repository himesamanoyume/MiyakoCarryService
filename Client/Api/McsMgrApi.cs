
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Api
{
    public static class McsMgrApi
    {
        /// <summary>
        /// 
        /// </summary>
        public static T GetMgr<T>() where T : BaseMgr
        {
            return MgrAccessor.Get<T>();
        }
    }
}