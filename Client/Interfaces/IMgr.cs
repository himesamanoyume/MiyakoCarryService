
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Interfaces
{
    internal interface IMgr
    {
        public HashSet<T> GetDatas<T>() where T : BaseData;
    }
}