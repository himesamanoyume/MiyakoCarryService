using System.Reflection;
using Fika.Core.Main.Players;
using HarmonyLib;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
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

        private static McsMgr McsMgr => McsMgrApi.GetMgr<McsMgr>();

        [PatchPrefix]
        public static void Prefix(FikaPlayer __instance, NetworkHealthSyncPacketStruct packet)
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