
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Interfaces
{
    public interface IMgr
    {
        public HashSet<T> GetDatas<T>() where T : BaseData;
    }
}