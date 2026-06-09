using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Interfaces;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class BaseMgr<T> : MonoBehaviour, IMgr where T : MonoBehaviour
    {
        protected GameLoop _gameloop;

        public virtual void Start()
        {
            _gameloop = GameLoop.Instance;
            _gameloop.Mgrs.Add(typeof(T), this);
            EventMgr.Subscribe<GameWorldStartedEvent>(OnGameWorldStarted, this);  
            EventMgr.Subscribe<GameWorldEndedEvent>(OnGameWorldEnded, this);
        }

        public static void Enable()
        {
            if (GameLoop.Instance.gameObject.GetComponent<T>() == null)
            {
                GameLoop.Instance.gameObject.AddComponent<T>();
            }
        }

        private void OnDestroy()
        {
            OnMgrDestroy();
        }

        protected virtual void OnMgrDestroy()
        {
            EventMgr.UnsubscribeAll(this); 
        }

        protected virtual void OnGameWorldStarted(GameWorldStartedEvent @event)  
        {  
            OnRaidStarted();  
        }  
    
        protected virtual void OnGameWorldEnded(GameWorldEndedEvent @event)  
        {  
            OnRaidEnded();  
        } 

        protected virtual void OnRaidStarted()
        {
            StopAllCoroutines();
        }

        protected virtual void OnRaidEnded()
        {
            StopAllCoroutines();
        }
    }
}