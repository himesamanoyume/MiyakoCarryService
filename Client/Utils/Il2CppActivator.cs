using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    internal static class Il2CppActivator
    {
        public static void Enable<T>() where T : MonoBehaviour => Enable(typeof(T));

        public static void Enable(Type type)
        {
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp(type))
            {
                ClassInjector.RegisterTypeInIl2Cpp(type);
            }

            var gameObject = GameLoop.Instance.gameObject;
            var il2CppType = ManagedArguments.ToIl2CppType(type);
            if (gameObject.GetComponent(il2CppType) == null)
            {
                gameObject.AddComponent(il2CppType);
            }
        }
    }
}