
using System.Collections;
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class DataMgr<T> : BaseMgr<T> where T : MonoBehaviour
    {
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