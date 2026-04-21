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
    public sealed class ApplyDamagePatch : ModulePatch
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

            if (!McsMgr.IsHost)
            {
                return;
            }

            if (damage <= 0)
            {
                return;
            }

            var attacker = damageInfo.Player?.iPlayer;

            if (attacker == null)
            {
                return;
            }

            var isMcsBotAttacker = McsMgr.IsMcsBotPlayer(attacker.ProfileId);
            var isMcsLeadAttacker = McsMgr.IsMcsLeadPlayer(attacker.ProfileId);
            var isMcsBotInjuredPlayer = McsMgr.IsMcsBotPlayer(___Player.ProfileId);
            var isMcsLeadInjuredPlayer = McsMgr.IsMcsLeadPlayer(___Player.ProfileId);

            var commonHp = __instance.GetBodyPartHealth(EBodyPart.Common);
            var headHp = __instance.GetBodyPartHealth(EBodyPart.Head);
            var chestHp = __instance.GetBodyPartHealth(EBodyPart.Chest);
            var isDead = commonHp.AtMinimum || headHp.AtMinimum || chestHp.AtMinimum;

            if (isMcsBotAttacker && isMcsLeadInjuredPlayer)
            {
                if (!isDead)
                {
                    return;
                }
                McsMgr.SendCompensation(___Player.ProfileId);
            }
            else if (isMcsBotInjuredPlayer && isMcsLeadAttacker)
            {
                var notMcsLeaderButIsFikaPlayer = attacker.Profile.Info.GroupId == "Fika";
                McsMgr.AddPunish(attacker.ProfileId, isDead ? 0.1560d : 0.0107d, isDead, notMcsLeaderButIsFikaPlayer);
            }
        }
    }
}