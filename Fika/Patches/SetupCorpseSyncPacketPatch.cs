using System.Reflection;
using Fika.Core.Main.Players;
using HarmonyLib;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Patches.Bots;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    /// <summary>
    /// 使服务端开启平衡限制时，清除护航的自带物品
    /// </summary>
    public class SetupCorpseSyncPacketPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(FikaPlayer), nameof(FikaPlayer.SetupCorpseSyncPacket));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Prefix(FikaPlayer __instance, NetworkHealthSyncPacketStruct packet)
        {
            if (!McsMgr.IsHost)
            {
                return;
            }

            if (!MiyakoCarryServicePlugin.McsPluginConfig.Server.BalanceRestriction)
            {
                return;
            }

            if (!McsMgr.IsMcsBotPlayer(__instance.ProfileId))
            {
                return;
            }

            PlayerOnDeadPatch.HandleBalanceRestriction(__instance);
        }
    }
}