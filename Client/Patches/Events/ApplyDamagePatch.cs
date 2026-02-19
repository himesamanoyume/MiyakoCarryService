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

            if (player == null)
            {
                return;
            }

            if (McsMgr.IsMcsBotPlayer(___Player.ProfileId))
            {
                var isMcsLeadPlayer = McsMgr.IsMcsLeadPlayer(player.ProfileId);
                var notMcsLeaderButIsFikaPlayer = player.Profile.Info.GroupId == "Fika" && !isMcsLeadPlayer;
                if (!(isMcsLeadPlayer || notMcsLeaderButIsFikaPlayer))
                {
                    return;
                }
                var commonHp = __instance.GetBodyPartHealth(EBodyPart.Common);
                var headHp = __instance.GetBodyPartHealth(EBodyPart.Head);
                var chestHp = __instance.GetBodyPartHealth(EBodyPart.Chest);
                var isDead = commonHp.AtMinimum || headHp.AtMinimum || chestHp.AtMinimum;
                McsMgr.AddPunish(player.ProfileId, isDead ? 0.1560d : 0.0107d, isDead, notMcsLeaderButIsFikaPlayer);
            }
        }
    }
}