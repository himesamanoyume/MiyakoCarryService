
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Interfaces;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    internal abstract class DataMgr<T> : BaseMgr<T>, IDataMgr  where T : MonoBehaviour
    {
        protected List<BaseData> _allDatas;
        public List<K> GetAllDatas<K>() where K : BaseData
        {
            var result = new List<K>();
            foreach (BaseData item in _allDatas)
            {
                if (item is K k)
                {
                    result.Add(k);
                }
            }
            return result;
        }
    }
}