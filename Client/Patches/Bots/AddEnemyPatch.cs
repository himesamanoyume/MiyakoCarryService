using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    internal sealed class AddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsGroup), nameof(BotsGroup.AddEnemy));

        [PatchPrefix]
        public static bool Prefix(IPlayer person, EBotEnemyCause cause)
        {
            if (person == null)
            {
                return true;
            }

            if (GameLoop.Instance.GetMgr<BotMgr>(EMgrType.BOT).IsMcsBossPlayer(person.ProfileId))
            {
                return false;
            }

            return true;
        }
    }
}