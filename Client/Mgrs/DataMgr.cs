
using System.Collections;
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    internal abstract class DataMgr<T> : BaseMgr<T>  where T : MonoBehaviour
    {
        protected HashSet<BaseData> _datas;
        public HashSet<K> GetDatas<K>() where K : BaseData
        {
            var result = new HashSet<K>();
            foreach (BaseData item in _datas)
            {
                if (item is K k)
                {
                    result.Add(k);
                }
            }
            return result;
        }
        protected McsMgr McsMgr {get; private set;}

        public override void Start()
        {
            base.Start();
            _datas = new HashSet<BaseData>();
            McsMgr = _gameloop.GetMgr<McsMgr>();
        }

        protected abstract IEnumerator ReloadDataLoop(float time);
        protected abstract IEnumerator LoadLootData(float time);
    }
}