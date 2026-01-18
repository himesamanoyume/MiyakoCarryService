
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    internal abstract class DataMgr<T> : BaseMgr<T>  where T : MonoBehaviour
    {
        protected List<BaseData> _datas;
        public List<K> GetDatas<K>() where K : BaseData
        {
            var result = new List<K>();
            foreach (BaseData item in _datas)
            {
                if (item is K k)
                {
                    result.Add(k);
                }
            }
            return result;
        }

        public override void Start()
        {
            base.Start();
            _datas = new List<BaseData>();
        }
    }
}