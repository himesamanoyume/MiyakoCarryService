using Il2CppInterop.Runtime.Injection;
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
                if (_instance != null)
                {
                    return _instance;
                }

                var type = typeof(T);
                if (!ClassInjector.IsTypeRegisteredInIl2Cpp(type))
                {
                    ClassInjector.RegisterTypeInIl2Cpp(type);
                }

                var gameObject = new GameObject("MiyakoCarryService" + type.Name)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                DontDestroyOnLoad(gameObject);

                _instance = gameObject.AddComponent(ManagedArguments.ToIl2CppType(typeof(T))).TryCast<T>();
                return _instance;
            }
        }

        public static bool Instantiated => _instance != null;

        public static void Clear()
        {
            _instance = null;
        }
    }
}
