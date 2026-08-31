using System;
using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    internal static class BotSettingUtils
    {
        private static readonly float[] _boostUpCoefficients = [0.55f, 0.70f, 0.85f, 0.95f, 1.0f];
        private static readonly float[] _weakenUpCoefficients = [1.45f, 1.30f, 1.15f, 1.05f, 1.0f];

        public static int GetCarryServiceLevel(int playerLevel)
        {
            return playerLevel switch
            {
                < 1 or > 78 => 5,
                <= 14 => 1,
                <= 29 => 2,
                <= 49 => 3,
                <= 69 => 4,
                _ => 5
            };
        }

        public static float ApplyBoostUp(float currentValue, float nativeValue, int carryServiceLevel)
        {
            var carryLevel = ClampLevel(carryServiceLevel);
            var baseValue = Math.Max(currentValue, nativeValue);
            var scaledValue = baseValue * _boostUpCoefficients[carryLevel - 1];
            return Math.Max(scaledValue, nativeValue);
        }

        public static float ApplyWeakenUp(float currentValue, float nativeValue, int carryServiceLevel)
        {
            var carryLevel = ClampLevel(carryServiceLevel);
            var baseValue = Math.Min(currentValue, nativeValue);
            var scaledValue = baseValue * _weakenUpCoefficients[carryLevel - 1];
            return Math.Min(scaledValue, nativeValue);
        }

        private static int ClampLevel(int carryServiceLevel)
        {
            return Mathf.Clamp(carryServiceLevel, 1, 5);
        }
    }
}
