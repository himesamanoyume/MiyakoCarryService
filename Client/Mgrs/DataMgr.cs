
using System;
using System.Collections;
using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Events;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class DataMgr<T> : BaseMgr<T> where T : MonoBehaviour
    {
        protected McsMgr McsMgr { get; private set; }
        

        public override void Start()
        {
            base.Start();
            _datas = new HashSet<BaseData>();
            McsMgr = _gameloop.GetMgr<McsMgr>();
        }

        protected abstract IEnumerator ReloadDataLoop(float time);
        protected abstract IEnumerator LoadLootData(float time);

        protected override void OnGameWorldStarted(GameWorldStartedEvent @event)
        {
            base.OnGameWorldStarted(@event);
            foreach (var data in _datas)
            {
                if (data is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        protected override void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            base.OnGameWorldEnded(@event);
        }
    }
}