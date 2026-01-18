using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Interfaces
{
    internal interface IDataMgr
    {
        public List<T> GetAllDatas<T>() where T : BaseData;
    }
}