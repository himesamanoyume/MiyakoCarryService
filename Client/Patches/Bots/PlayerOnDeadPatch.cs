
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
    /// 使服务端开启平衡限制时，清除护航的自带物品
    /// </summary>
    public sealed class PlayerOnDeadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.OnDead));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Prefix(Player __instance, EDamageType damageType)
        {
            if (!Tools.IsHost)
            {
                return;
            }

            if (!MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction)
            {
                return;
            }

            if (!McsMgr.IsMcsBotPlayer(__instance.ProfileId))
            {
                return;
            }

            var mcsBotPlayerData = __instance.AIData.BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }

            mcsBotPlayerData.HandleBalanceRestriction();
        }
    }
}