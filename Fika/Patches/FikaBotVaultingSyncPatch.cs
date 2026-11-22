using System.Reflection;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace MiyakoCarryService.Fika.Patches
{
    /// <summary>
    /// 实现Bot的翻越行为也会被同步
    /// </summary>
    public sealed class FikaBotVaultingSyncPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(FikaBot), nameof(FikaBot.OnVaulting));

        [PatchPostfix]
        public static void Postfix(FikaBot __instance)
        {
            var packetSender = __instance.BotPacketSender;
            if (packetSender == null || packetSender.NetworkManager == null)
            {
                return;
            }

            var vaultingParameters = __instance.VaultingParameters;
            var packet = new VaultPacket(
                vaultingParameters.GetVaultingStrategy(),
                vaultingParameters.MaxWeightPointPosition,
                vaultingParameters.VaultingHeight,
                vaultingParameters.VaultingLength,
                __instance.MovementContext.VaultingSpeed,
                vaultingParameters.BehindObstacleRatio,
                vaultingParameters.AbsoluteForwardVelocity);
            packetSender.NetworkManager.SendPlayerPacket(in packet, __instance.NetId, DeliveryMethod.ReliableOrdered, true);
        }
    }
}