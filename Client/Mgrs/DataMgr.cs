
using System;
using System.Collections;
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class DataMgr : BaseMgr
    {
        protected HashSet<BaseData> _datas;
        protected McsMgr McsMgr { get; private set; }

        public override void Start()
        {
            base.Start();
            _datas = new HashSet<BaseData>();
            McsMgr = MgrAccessor.Get<McsMgr>();
        }

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

        public override void OnGameWorldStarted(GameWorldStartedEvent @event)
        {
            base.OnGameWorldStarted(@event);
        }

        public override void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            base.OnGameWorldEnded(@event);
            DataClear();
        }

        public IEnumerator ReloadDataLoop(float time, Action action)
        {
            var waitTime = new WaitForSeconds(time);
            while (true)
            {
                yield return waitTime;
                LoadData(action);
            }
        }

        public void LoadData(Action action)
        {
            if (Gameloop.IsVaildGameWorld)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    MiyakoCarryServicePlugin.Logger.LogError(e);
                }
            }
        }

        public void DataClear()
        {
            foreach (var data in _datas)
            {
                if (data is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _datas.Clear();
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            DataClear();
        }
    }
}