using System.Reflection;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 监测玩家对护航的误伤情况
    /// </summary>
    internal sealed class ApplyDamagePatch : ModulePatch
    {
        private static McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }
        
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage));

        [PatchPostfix]
        public static void Postfix(ActiveHealthController __instance, Player ___Player, EBodyPart bodyPart, float damage, DamageInfoStruct damageInfo)
        {
            if (___Player == null)
            {
                return;
            }

            if (!McsMgr.IsHost || damage <= 0)
            {
                return;
            }

            var player = damageInfo.Player?.iPlayer;

            if (player != null)
            {
                return;
            }

            if (McsMgr.IsMcsBotPlayer(___Player.ProfileId))
            {
                var isMcsLeadPlayer = McsMgr.IsMcsLeadPlayer(player.ProfileId);
                var notMcsLeaderButIsFikaPlayer = player.Profile.Info.GroupId == "Fika" && !isMcsLeadPlayer;
                var commonHp = __instance.GetBodyPartHealth(EBodyPart.Common);
                McsMgr.AddPunish(player.ProfileId, commonHp.AtMinimum ? -1.56f : -0.15f, notMcsLeaderButIsFikaPlayer);
            }
        }
    }
}