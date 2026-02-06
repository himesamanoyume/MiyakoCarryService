
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots;

internal sealed class ApplyDamagePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage));

    private static SquadMgr SquadMgr
    { 
        get
        {
            return field ??= GameLoop.Instance.GetMgr<SquadMgr>();
        }
    }

    [PatchPrefix]
    public static void PatchPrefix(Player ___Player, EBodyPart bodyPart, ref float damage, DamageInfoStruct damageInfo)
    {
        if (___Player == null || !___Player.IsAI)
        {
            return;
        }

        if (!___Player.AIData.BotOwner.IsMcsBotPlayer)
        {
            return;
        }

        // 先让护航保持无敌
        // if (damageInfo.Player == null)
        // {
        //     return;
        // }

        // if (damageInfo.Player.iPlayer == null || SquadMgr.IsMcsLeadPlayer(damageInfo.Player.iPlayer.ProfileId))
        // {
        //     return;
        // }

        damage = 0;
    }
}