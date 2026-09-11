using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    internal static class BotSettingUtils
    {
        private static readonly float[] _boostUpCoefficients = [0.42f, 0.55f, 0.75f, 0.90f, 1.0f];
        private static readonly float[] _weakenUpCoefficients = [1.667f, 1.6f, 1.35f, 1.15f, 1.0f];
        private static readonly float[] _predictDistances = [300f, 400f, 500f, 600f, 700f];
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

        public static float GetPredictDistance(int carryServiceLevel)
        {
            return _predictDistances[ClampLevel(carryServiceLevel) - 1];
        }

        public static float GetPredictConfidence(float distance, int carryServiceLevel)
        {
            var predictDistance = GetPredictDistance(carryServiceLevel);
            if (distance <= predictDistance)
            {
                return 1f;
            }

            var falloffDistance = predictDistance * 0.5f;
            if (falloffDistance <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(1f - ((distance - predictDistance) / falloffDistance));
        }

        public static float GetBoostScaled(float strongValue, int carryServiceLevel)
        {
            return GetBoostScaled(strongValue * _boostUpCoefficients[0], strongValue, carryServiceLevel);
        }

        public static float GetBoostScaled(float weakValue, float strongValue, int carryServiceLevel)
        {
            var carryLevel = ClampLevel(carryServiceLevel);
            var t = (_boostUpCoefficients[carryLevel - 1] - _boostUpCoefficients[0]) / (_boostUpCoefficients[^1] - _boostUpCoefficients[0]);
            return Mathf.Lerp(weakValue, strongValue, t);
        }

        public static float GetWeakenScaled(float strongValue, int carryServiceLevel)
        {
            return GetWeakenScaled(strongValue * _weakenUpCoefficients[0], strongValue, carryServiceLevel);
        }

        public static float GetWeakenScaled(float weakValue, float strongValue, int carryServiceLevel)
        {
            var carryLevel = ClampLevel(carryServiceLevel);
            var w = (_weakenUpCoefficients[carryLevel - 1] - _weakenUpCoefficients[^1]) / (_weakenUpCoefficients[0] - _weakenUpCoefficients[^1]);
            return Mathf.Lerp(strongValue, weakValue, w);
        }

        public static int GetWeakenScaledInt(float weakValue, int carryServiceLevel)
        {
            return Mathf.RoundToInt(GetWeakenScaled(weakValue, 0f, carryServiceLevel));
        }

        private static int ClampLevel(int carryServiceLevel)
        {
            return Mathf.Clamp(carryServiceLevel, 1, 5);
        }
    }
}