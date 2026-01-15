using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    internal sealed class AddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotMemoryClass), nameof(BotMemoryClass.AddEnemy));

        [PatchPrefix]
        public static bool Prefix(BotMemoryClass __instance, [NotNull] IPlayer enemy, BotSettingsClass groupInfo, bool onActivation)
        {
            if (enemy == null)
            {
                return true;
            }

            if (enemy.ProfileId == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                __instance.DeleteInfoAboutEnemy(enemy);
                return false;
            }

            return true;
        }
    }
}