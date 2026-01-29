using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 借鉴friendlyPmc
    /// </summary>
    internal sealed class PlayerSayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.Say));

        private static SquadMgr SquadMgr
        { 
            get
            {
                return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
            }
        }

        [PatchPostfix]
        public static void Postfix(Player __instance, EPhraseTrigger phrase, bool demand = false, float delay = 0f, ETagStatus mask = 0, int probability = 100, bool aggressive = false)
        {
            if (SquadMgr.IsMcsBossPlayer(__instance.ProfileId) || SquadMgr.IsMcsBotPlayer(__instance.ProfileId))
            {
                return;
            }

            foreach (var botOwner in SquadMgr.GetAllMcsBotPlayer())
            {
                if (botOwner.HearingSensor.method_6(__instance.Transform.position, 50f, out var dist))
                {
                    botOwner.BotsGroup.ReportAboutEnemy(__instance, EEnemyPartVisibleType.Sence, botOwner);
                }
            }
        }
    }
}