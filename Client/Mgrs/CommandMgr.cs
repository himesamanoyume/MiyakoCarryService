

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using HarmonyLib;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Networking.Packets.Command;
using MiyakoCarryService.Client.Utils;
using TMPro;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class CommandMgr : BaseMgr<CommandMgr>
    {
        public sealed override void Start()
        {
            base.Start();
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkCreated);
                _handleActionsMap = new()
                {
                    {ECommandPacketType.Teleport, HandleTeleport},
                };
            }
        }

        private GamePlayerOwner _gamePlayerOwner
        {
            set
            {
                field = value;
            }
            get
            {
                return field ??= Singleton<GameWorld>.Instance.MainPlayer.GetComponentInChildren<GamePlayerOwner>();
            }
        }

        private McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

        private List<MongoID> _mcsBotPlayerIds = new();
        private ConcurrentDictionary<MongoID, Player> _mcsBotPlayers = new();
        private Dictionary<ECommandPacketType, Action<CommandPacket>> _handleActionsMap;

        public void OnFikaNetworkCreated(FikaNetworkManagerCreatedEvent fikaEvent)
        {
            MiyakoCarryServicePlugin.Logger.LogWarning($"OnFikaNetworkCreated，开始注册数据包");
            fikaEvent.Manager.RegisterPacket<CommandPacket>(OnCommandPacketReceived);
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
            MiyakoCarryServicePlugin.Logger.LogWarning($"IsServer: {FikaBackendUtils.IsServer}, 接收到CommandPacket");
            if (!FikaBackendUtils.IsServer)
            {
                MiyakoCarryServicePlugin.Logger.LogWarning($"并不是 FikaServer");
                return;
            }

            var server = Singleton<IFikaNetworkManager>.Instance;

            server.CoopHandler.Players.TryGetValue(packet.McsLeadPlayerNetId, out FikaPlayer mcsLeadPlayer);

            if (mcsLeadPlayer == null)
            {
                MiyakoCarryServicePlugin.Logger.LogWarning($"mcsLeadPlayer 为空");
                return;
            }
            else
            {
                MiyakoCarryServicePlugin.Logger.LogWarning($"mcsLeadPlayer：{mcsLeadPlayer.Profile.Nickname}");
            }

            if (server.CoopHandler.Players.TryGetValue(packet.McsBotPlayerNetId, out FikaPlayer mcsBotPlayer))  
            {  
                mcsBotPlayer.Teleport(mcsLeadPlayer.Position);
                MiyakoCarryServicePlugin.Logger.LogWarning($"对 mcsBotPlayer: {mcsBotPlayer.Profile.Nickname} 执行传送至: {mcsLeadPlayer.Position}");
            }
            else
            {
                MiyakoCarryServicePlugin.Logger.LogWarning($"未能通过 McsBotPlayerNetId 找到 mcsBotPlayer");
            }
        }

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                if (FikaBackendUtils.IsServer)
                {
                    var fikaServer = Singleton<FikaServer>.Instance;
                    fikaServer.RegisterPacket<CommandPacket>(OnCommandPacketReceived); 
                }
            }
            _mcsBotPlayerIds = McsRequestHandler.GetMcsBotPlayerIds();
            _gamePlayerOwner = null;
        }

        protected sealed override void OnRaidEnded()
        {
            base.OnRaidEnded();
            _mcsBotPlayerIds.Clear();
            _mcsBotPlayers.Clear();
            _gamePlayerOwner = null;
        }

        void Update()
        {
            if (!_gameloop.IsVaildGameWorld)
            {
                return;
            }

            if (KeyInput.BetterIsDown(MiyakoCarryServicePlugin.CommandHotKey.Value))
            {
                BuildMainCommandMenu();
            }
        }

        private Player TryGetMcsBotPlayer(MongoID mcsBotPlayerId)
        {
            return _mcsBotPlayers.GetOrAdd(mcsBotPlayerId, _ => Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(mcsBotPlayerId));
        }

        public void BuildMainCommandMenu()
        {
            if (_gamePlayerOwner == null || _mcsBotPlayerIds.Count == 0)
            {
                return;
            }

            var actionsReturnClass = new ActionsReturnClass
            {
                Actions = new()
            };

            var mcsBotPlayers = new List<Player>();
            foreach (var mcsBotPlayerId in _mcsBotPlayerIds)
            {
                var mcsBotPlayer = TryGetMcsBotPlayer(mcsBotPlayerId);
                if (mcsBotPlayer == null)
                {
                    continue;
                }

                mcsBotPlayers.Add(mcsBotPlayer);
            }

            actionsReturnClass.CurrentActionChanged.Bind(OnCurrentActionChanged);

            actionsReturnClass.Actions.Add(TeamCommand(BuildTeamCommandMenu, mcsBotPlayers));

            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                actionsReturnClass.Actions.Add(MemberCommand(BuildMemberCommandMenu, mcsBotPlayer));
            }

            actionsReturnClass.Actions.Add(CancelCommand(CloseCommandMenuAction));

            if (actionsReturnClass != null)
            {
                actionsReturnClass.InitSelected();
            }

            _gamePlayerOwner.AvailableInteractionState.Value = actionsReturnClass;
        }

        public void BuildTeamCommandMenu()
        {
            if (_gamePlayerOwner == null || _mcsBotPlayerIds.Count == 0)
            {
                return;
            }

            var actionsReturnClass = new ActionsReturnClass
            {
                Actions = new()
            };

            actionsReturnClass.CurrentActionChanged.Bind(OnCurrentActionChanged);

            // actionsReturnClass.Actions.Add(TeamHoldCommand(CloseCommandMenu));
            actionsReturnClass.Actions.Add(TeamForceTeleportCommand(ForceTeleportCommandAction));
            actionsReturnClass.Actions.Add(CancelCommand(CloseCommandMenuAction));

            if (actionsReturnClass != null)
            {
                actionsReturnClass.InitSelected();
            }

            _gamePlayerOwner.AvailableInteractionState.Value = actionsReturnClass;
        }

        private void OnCurrentActionChanged()
        {
            if (!Singleton<CommonUI>.Instantiated)
            {
                return;
            }

            var actionPanel = Singleton<CommonUI>.Instance.EftBattleUIScreen?.ActionPanel;
            if (actionPanel == null)
            {
                return;
            }

            var itemName = AccessTools.Field(typeof(ActionPanel), "_itemName").GetValue(actionPanel) as TextMeshProUGUI;
            
            var selectedAction = _gamePlayerOwner?.AvailableInteractionState?.Value?.SelectedAction;
            if (selectedAction == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(selectedAction.TargetName))
            {
                return;
            }

            itemName.text = selectedAction.TargetName.McsLocalized().ToUpper();
        }

        public ActionsTypesClass TeamCommand(Action action, List<Player> mcsBotPlayers)
        {
            return new ActionsTypesClass
            {
                Name = Locales.TEAMCOMMAND_NAME,
                TargetName = Locales.TEAMCOMMAND_TARGETNAME,
                Disabled = mcsBotPlayers.All(p => !p.HealthController.IsAlive),
                // p中的某个元素为空导致菜单无法打开
                Action = action
            };
        }

        public ActionsTypesClass MemberCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = mcsBotPlayer.Profile.Info.Nickname,
                TargetName = Locales.MEMBERCOMMAND_TARGETNAME,
                Disabled = !mcsBotPlayer.HealthController.IsAlive,
                // p中的某个元素为空导致菜单无法打开
                Action = new Action(() =>
                {
                    action(mcsBotPlayer);
                })
            };
        }

        public ActionsTypesClass TeamHoldCommand(Action action)
        {
            return new ActionsTypesClass
            {
                Name = "全队停留在这",
                TargetName = "指定一个位置让全部队友驻留",
                Disabled = false,
                Action = action
            };
        }

        public ActionsTypesClass TeamForceTeleportCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = Locales.TEAMFORCETELEPORTCOMMAND_NAME,
                TargetName = Locales.TEAMFORCETELEPORTCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    foreach (var mcsBotPlayerId in _mcsBotPlayerIds)
                    {
                        var mcsBotPlayer = TryGetMcsBotPlayer(mcsBotPlayerId);
                        if (mcsBotPlayer == null)
                        {
                            continue;
                        }

                        if (!mcsBotPlayer.HealthController.IsAlive)
                        {
                            continue;
                        }

                        action(mcsBotPlayer);
                    }
                })
            };
        }

        public void BuildMemberCommandMenu(Player mcsBotPlayer)
        {
            if (_gamePlayerOwner == null || _mcsBotPlayerIds.Count == 0)
            {
                return;
            }

            var actionsReturnClass = new ActionsReturnClass
            {
                Actions = new()
            };

            actionsReturnClass.CurrentActionChanged.Bind(OnCurrentActionChanged);

            // actionsReturnClass.Actions.Add(HoldCommand(CloseCommandMenu));
            actionsReturnClass.Actions.Add(ForceTeleportCommand(ForceTeleportCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(CancelCommand(CloseCommandMenuAction));

            if (actionsReturnClass != null)
            {
                actionsReturnClass.InitSelected();
            }

            _gamePlayerOwner.AvailableInteractionState.Value = actionsReturnClass;
        }

        public ActionsTypesClass HoldCommand(Action action)
        {
            return new ActionsTypesClass
            {
                Name = "停留在这",
                TargetName = "指定一个位置让队友驻留",
                Disabled = false,
                Action = action
            };
        }

        public ActionsTypesClass ForceTeleportCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = Locales.FORCETELEPORTCOMMAND_NAME,
                TargetName = Locales.FORCETELEPORTCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    action(mcsBotPlayer);
                })
            };
        }

        public ActionsTypesClass CancelCommand(Action action)
        {
            return new ActionsTypesClass
            {
                Name = Locales.CANCELCOMMAND_NAME,
                TargetName = Locales.CANCELCOMMAND_TARGETNAME,
                Disabled = false,
                Action = action
            };
        }

        public void CloseCommandMenuAction()
        {
            _gamePlayerOwner.AvailableInteractionState.Value = new ActionsReturnClass();
        }

        public void ForceTeleportCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                if (FikaBackendUtils.IsServer)
                {
                    var botOwner = mcsBotPlayer.AIData.BotOwner;
                    mcsBotPlayer.Teleport(botOwner.GetMcsBotData().LeadPlayer.Position);
                    if (!MiyakoCarryServicePlugin.SAINInstalled)
                    {
                        botOwner.Memory.GoalTarget.Clear();
                        botOwner.Memory.GoalEnemy = null;
                    }
                }
                else
                {
                    var mcsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                    if (mcsLeadPlayer is FikaPlayer fikaMcsLeadPlayer && mcsBotPlayer is FikaPlayer fikaMcsBotPlayer)
                    {
                        MiyakoCarryServicePlugin.Logger.LogWarning($"fikaMcsLeadPlayer: {fikaMcsLeadPlayer.Profile.Nickname}, fikaMcsBotPlayer: {fikaMcsBotPlayer.Profile.Nickname}");
                        var packet = new CommandPacket(ECommandPacketType.Teleport)
                        {
                            McsLeadPlayerNetId = fikaMcsLeadPlayer.NetId,
                            McsBotPlayerNetId = fikaMcsBotPlayer.NetId
                        };
                        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                mcsBotPlayer.Teleport(botOwner.GetMcsBotData().LeadPlayer.Position);
                if (!MiyakoCarryServicePlugin.SAINInstalled)
                {
                    botOwner.Memory.GoalTarget.Clear();
                    botOwner.Memory.GoalEnemy = null;
                }
            }
            CloseCommandMenuAction();
        }
    }
}