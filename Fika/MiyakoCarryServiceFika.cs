using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using MiyakoCarryService.Client;
using MiyakoCarryService.Client.Enums;
using System;
using MiyakoCarryService.Fika.Packets;
using MiyakoCarryService.Client.Mgrs;
using Fika.Core.Modding.Events;
using Fika.Core.Main.Utils;
using Comfort.Common;
using Fika.Core.Networking;
using Fika.Core.Main.Players;
using Fika.Core.Modding;
using EFT;
using Fika.Core.Networking.LiteNetLib;

namespace MiyakoCarryService.Fika
{
    [BepInPlugin(McsFikaGUID, McsFikaPluginName, BepInExClientVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    [BepInDependency(BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(McsGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(FikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MiyakoCarryServiceFikaPlugin : BaseUnityPlugin
    {
        public const string BepInExClientVersion = "0.2.2.0";
        public const string McsGUID = "top.himesamanoyume.miyakocarryservice";
        public const string FikaGUID = "com.fika.core";
        public const string McsFikaGUID = "top.himesamanoyume.miyakocarryservicefika";
        public const string BigBrainGUID = "xyz.drakia.bigbrain";
        public const string McsFikaPluginName = "姫様の夢 MiyakoCarryServiceFika";
        public static MiyakoCarryServiceFikaPlugin Instance;
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MiyakoCarryServiceFika");
        public static bool McsInstalled { get; private set; } = false;
        public static bool FikaInstalled { get; private set; } = false;
        public static bool IsFikaHeadless { get; private set; } = false;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            McsInstalled = !CheckPlugin([McsGUID]);
            FikaInstalled = !CheckPlugin([FikaGUID]);
            IsFikaHeadless = !CheckPlugin(["com.fika.headless"]);

            if (!McsInstalled)
            {
                return;
            }

            if (!FikaInstalled)
            {
                return;
            }

            EnableAllPatches();

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
            _handleActionsMap = new()
            {
                {ECommandPacketType.Teleport, HandleTeleport},
            };
            
        }

        public bool CheckPlugin(List<string> pluginList)
        {
            var pluginInfos = new List<PluginInfo>(Chainloader.PluginInfos.Values);

            foreach (PluginInfo Info in pluginInfos)
            {
                if (pluginList.Contains(Info.Metadata.GUID))
                {
                    return false;
                }
            }
            return true;
        }

        public bool CheckUnsupportedPlugin()
        {
            return CheckPlugin([]);
        }

        private void EnableAllPatches()
        {
            
        }

        private Dictionary<ECommandPacketType, Action<CommandPacket>> _handleActionsMap;

        private CommandMgr CommandMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<CommandMgr>();
            }
        }

        public void OnFikaNetworkCreated(FikaNetworkManagerCreatedEvent fikaEvent)
        {
            Logger.LogWarning($"OnFikaNetworkCreated，开始注册数据包");
            fikaEvent.Manager.RegisterPacket<CommandPacket>(OnCommandPacketReceived);
            CommandMgr.HandleFikaEventsMap.TryAdd(ECommandPacketType.Teleport, SendTeleportCommandPacket);
        }

        public void OnCommandPacketReceived(CommandPacket packet)  
        {  
            if (_handleActionsMap.TryGetValue(packet.CommandType, out var action))
            {
                action(packet);
            }
        }

        private void HandleTeleport(CommandPacket packet)
        {
            Logger.LogWarning($"IsServer: {FikaBackendUtils.IsServer}, 接收到CommandPacket");
            if (!FikaBackendUtils.IsServer)
            {
                Logger.LogWarning($"并不是 FikaServer");
                return;
            }

            var server = Singleton<IFikaNetworkManager>.Instance;

            server.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null)
            {
                Logger.LogWarning($"mcsLeadPlayer 为空");
                return;
            }
            else
            {
                Logger.LogWarning($"mcsLeadPlayer：{mcsLeadPlayer.Profile.Nickname}");
            }

            if (server.CoopHandler.Players.TryGetValue(packet.McsBotPlayerNetId, out FikaPlayer mcsBotPlayer))  
            {  
                mcsBotPlayer.Teleport(mcsLeadPlayer.Position);
                Logger.LogWarning($"对 mcsBotPlayer: {mcsBotPlayer.Profile.Nickname} 执行传送至: {mcsLeadPlayer.Position}");
            }
            else
            {
                Logger.LogWarning($"未能通过 McsBotPlayerNetId 找到 mcsBotPlayer");
            }
        }

        public void SendTeleportCommandPacket(Player mcsBotPlayer)
        {
            var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
            {
                Logger.LogWarning($"fikaMcsLeadPlayer: {fikaMcsLeadPlayer.Profile.Nickname}, fikaMcsBotPlayer: {fikaMcsBotPlayer.Profile.Nickname}");
                var packet = new CommandPacket(ECommandPacketType.Teleport)
                {
                    McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                    McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}
