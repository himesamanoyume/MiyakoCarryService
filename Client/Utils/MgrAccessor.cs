using MiyakoCarryService.Client.Mgrs;

namespace MiyakoCarryService.Client.Utils
{
    internal static class MgrAccessor
    {
        public static T Get<T>() where T : BaseMgr
        {
            return GameLoop.Instance.GetMgr<T>();
        }
    }
}