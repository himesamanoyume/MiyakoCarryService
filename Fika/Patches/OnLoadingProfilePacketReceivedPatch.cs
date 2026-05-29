using System.Reflection;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.Packets.Backend;
using HarmonyLib;
using MiyakoCarryService.Client;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    /// <summary>
    /// 玩家加入时立即进行一次护航信息获取
    /// </summary>
    public class OnLoadingProfilePacketReceivedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(FikaServer), "OnLoadingProfilePacketReceived");

        [PatchPostfix]
        public static void Postfix(LoadingProfilePacket packet, NetPeer peer)
        {
            TasksExtensions.HandleExceptions(GameLoop.Instance.SpawnMcsBotPlayer());
        }
    }
}