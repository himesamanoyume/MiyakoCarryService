
using MiyakoCarryService.Client.Interfaces;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    internal abstract class BaseMgr<T> : MonoBehaviour, IMgr where T : MonoBehaviour
    {
        protected GameLoop _gameloop;

        public virtual void Start()
        {
            _gameloop = GameLoop.Instance;
            _gameloop.Mgrs.Add(typeof(T), this);
            _gameloop.OnGameWorldStart += OnRaidStarted;
            _gameloop.OnGameWorldDestory += OnRaidEnded;
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
            _gameloop.OnGameWorldStart -= OnRaidStarted;
            _gameloop.OnGameWorldDestory -= OnRaidEnded;
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