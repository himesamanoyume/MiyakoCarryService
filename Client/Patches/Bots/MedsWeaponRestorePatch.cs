using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>
    /// 护航治疗收尾后补一次回枪
    /// </summary>
    public sealed class MedsWeaponRestorePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player.MedsController.MedsInHandsOperation), nameof(Player.MedsController.MedsInHandsOperation.HideWeaponComplete));

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(Player.MedsController.MedsInHandsOperation __instance)
        {
            var player = __instance?._controller?._player;
            if (player == null || !McsMgr.IsMcsBotPlayer(player.ProfileId))
            {
                return;
            }

            if (player.ProcessStatus != Player.EProcessStatus.None)
            {
                return;
            }

            player.TrySetLastEquippedWeapon(true);
        }
    }
}