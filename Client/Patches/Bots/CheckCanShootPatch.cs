using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Bots
{
    /// <summary>  
    /// 安装SAIN且护航使用固定武器时，射击射线原点极易被固定武器本体遮挡导致无法射击，因此使其在敌人可见时强制设CanShoot为true
    /// </summary>  
    public sealed class CheckCanShootPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EnemyPart), nameof(EnemyPart.CheckCanShoot));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(EnemyPart __instance, BotOwner botOwner, EnemyPartVision partVision, bool checkOnlyIfVisible = true)
        {
            if (botOwner == null || __instance.CanShoot)
            {
                return;
            }

            if (!McsMgr.IsMcsBotPlayer(botOwner.ProfileId))
            {
                return;
            }

            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null || !mcsBotPlayerData.HasDecision(Decisions.ShouldUseStationaryWeapon))
            {
                return;
            }

            if (partVision != null && partVision.Visible)
            {
                __instance.CanShoot = true;
                __instance._canShootLastValue = true;
            }
        }
    }
}