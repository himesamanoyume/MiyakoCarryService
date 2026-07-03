using System;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Interfaces;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class BaseMgr : MonoBehaviour, IMgr
    {
        public GameLoop Gameloop;

        public virtual void Start()
        {
            Gameloop = GameLoop.Instance;
            Gameloop.Mgrs.Add(GetType(), this);
            EventMgr.Subscribe<GameWorldStartedEvent>(OnGameWorldStarted, this);
            EventMgr.Subscribe<GameWorldEndedEvent>(OnGameWorldEnded, this);
        }

        public static void Enable(Type type)
        {
            if (GameLoop.Instance.gameObject.GetComponent(type) == null)
            {
                GameLoop.Instance.gameObject.AddComponent(type);
            }
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