using BepInEx.Configuration;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    public static class KeyInput
    {
        public static bool BetterIsPressed(KeyboardShortcut key)
        {
            if (!Input.GetKey(key.MainKey))
            {
                return false;
            }

            foreach (var modifier in key.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool BetterIsDown(KeyboardShortcut key)
        {
            if (!Input.GetKeyDown(key.MainKey))
            {
                return false;
            }

            foreach (var modifier in key.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool KeyDown(KeyboardShortcut key, ConfigEntry<bool> configEntry)
        {
            var isDown = BetterIsDown(key);
            if (isDown)
            {
                configEntry.Value = !configEntry.Value;
            }
            return isDown;
        }
    }
}
