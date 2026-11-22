using System;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class BaseMgr : MonoBehaviour
    {
        protected BaseMgr(IntPtr pointer) : base(pointer)
        {
        }

        public GameLoop Gameloop;

        public void Start()
        {
            try
            {
                Gameloop = GameLoop.Instance;
                Gameloop.Mgrs.Add(GetType(), this);
                EventMgr.Subscribe<GameWorldStartedEvent>(OnGameWorldStarted, this);
                EventMgr.Subscribe<GameWorldEndedEvent>(OnGameWorldEnded, this);

                OnMgrStart();
            }
            catch (Exception ex)
            {
                McsLogger.LogError($"[BaseMgr.Start] {GetType().FullName} {ex}");
            }
        }

        protected virtual void OnMgrStart()
        {
            
        }

        private void OnDestroy()
        {
            OnMgrDestroy();
        }

        public virtual void OnMgrDestroy()
        {
            EventMgr.UnsubscribeAll(this);
            OnRaidEnded();
        }

        public virtual void OnGameWorldStarted(GameWorldStartedEvent @event)
        {
            OnRaidStarted();
        }

        public virtual void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            OnRaidEnded();
        }

        public virtual void OnRaidStarted()
        {
            StopAllCoroutines();
        }

        public virtual void OnRaidEnded()
        {
            StopAllCoroutines();
        }
    }
}