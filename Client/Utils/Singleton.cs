using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    public class MiyakoCarryServiceSingleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance = null;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType(typeof(T)) as T;
                    if (_instance == null)
                    {
                        var obj = new GameObject();
                        _instance = (T)obj.AddComponent(typeof(T));
                        obj.hideFlags = HideFlags.DontSave;
                        obj.name = "MiyakoCarryService" + typeof(T).Name;
                    }
                }
                return _instance;
            }
        }

        public static bool Instantiated => _instance != null;

        public virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (_instance == null)
            {
                _instance = this as T;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public virtual void Destroy()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Destroy();
        }
    }
}
