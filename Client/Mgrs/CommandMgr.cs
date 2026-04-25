

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using TMPro;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class CommandMgr : BaseMgr<CommandMgr>
    {
        public sealed override void Start()
        {
            base.Start();
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
        public Dictionary<ECommandPacketType, Action<Player, Vector3?>> HandleFikaEventsMap = new();

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
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

            actionsReturnClass.Actions.Add(TeamRegroupCommand(RegroupCommandAction));
            actionsReturnClass.Actions.Add(TeamGoToPointCommand(GoToPointCommandAction));
            actionsReturnClass.Actions.Add(TeamHoldPositionCommand(HoldPositionCommandAction));
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

        public ActionsTypesClass TeamRegroupCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = "全队集结",
                TargetName = "让所有护航跟随老板",
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

        public ActionsTypesClass TeamGoToPointCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = "全队前往",
                TargetName = "让所有护航前往指定地点",
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

        public ActionsTypesClass TeamHoldPositionCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = "全队驻守",
                TargetName = "让全部队友停留在原地",
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
        public void RegroupCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                if (McsMgr.IsHost)
                {
                    var botOwner = mcsBotPlayer.AIData.BotOwner;
                    botOwner.GetMcsBotData().ShouldGoToPoint = false;
                    botOwner.GetMcsBotData().ShouldHoldPosition = false;
                    botOwner.TalkMsg(EPhraseTrigger.FollowMe);
                }
                else
                {
                    if (HandleFikaEventsMap.TryGetValue(ECommandPacketType.Regroup, out var action))
                    {
                        action(mcsBotPlayer, new Vector3());
                    }
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.GetMcsBotData().ShouldGoToPoint = false;
                botOwner.GetMcsBotData().ShouldHoldPosition = false;
                botOwner.TalkMsg(EPhraseTrigger.FollowMe);
            }
            CloseCommandMenuAction();
        }

        public void GoToPointCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                if (McsMgr.IsHost)
                {
                    var botOwner = mcsBotPlayer.AIData.BotOwner;
                    botOwner.GetMcsBotData().ShouldGoToPoint = true;
                    botOwner.TalkMsg(EPhraseTrigger.Going);
                }
                else
                {
                    if (HandleFikaEventsMap.TryGetValue(ECommandPacketType.GoToPoint, out var action))
                    {
                        if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue))
                        {
                            action(mcsBotPlayer, raycastHit.point);
                        }
                    }
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.GetMcsBotData().ShouldGoToPoint = true;
                botOwner.TalkMsg(EPhraseTrigger.Going);
            }
            CloseCommandMenuAction();
        }

        public void HoldPositionCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                if (McsMgr.IsHost)
                {
                    var botOwner = mcsBotPlayer.AIData.BotOwner;
                    botOwner.StopMove();
                    botOwner.GetMcsBotData().ShouldHoldPosition = true;
                    botOwner.TalkMsg(EPhraseTrigger.HoldPosition);
                }
                else
                {
                    if (HandleFikaEventsMap.TryGetValue(ECommandPacketType.HoldPosition, out var action))
                    {
                        action(mcsBotPlayer, new Vector3());
                    }
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                botOwner.GetMcsBotData().ShouldHoldPosition = true;
                botOwner.TalkMsg(EPhraseTrigger.HoldPosition);
            }
            CloseCommandMenuAction();
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

            actionsReturnClass.Actions.Add(RegroupCommand(RegroupCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(GoToPointCommand(GoToPointCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(HoldPositionCommand(HoldPositionCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(ForceTeleportCommand(ForceTeleportCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(CancelCommand(CloseCommandMenuAction));

            if (actionsReturnClass != null)
            {
                actionsReturnClass.InitSelected();
            }

            _gamePlayerOwner.AvailableInteractionState.Value = actionsReturnClass;
        }

        public ActionsTypesClass RegroupCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = "集结",
                TargetName = "命令护航跟随老板",
                Disabled = false,
                Action = new Action(() =>
                {
                    action(mcsBotPlayer);
                })
            };
        }

        public ActionsTypesClass GoToPointCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = "前往",
                TargetName = "命令护航前往指定地点",
                Disabled = false,
                Action = new Action(() =>
                {
                    action(mcsBotPlayer);
                })
            };
        }

        public ActionsTypesClass HoldPositionCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = "驻守",
                TargetName = "指定其停留在原地",
                Disabled = false,
                Action = new Action(() =>
                {
                    action(mcsBotPlayer);
                })
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
                if (McsMgr.IsHost)
                {
                    var botOwner = mcsBotPlayer.AIData.BotOwner;
                    botOwner.StopMove();
                    botOwner.Mover.AllowTeleport();
                    mcsBotPlayer.Teleport(botOwner.GetMcsBotData().LeadPlayer.Position, true);
                    botOwner.TalkMsg(EPhraseTrigger.Regroup);
                    if (!MiyakoCarryServicePlugin.SAINInstalled)
                    {
                        botOwner.Memory.GoalTarget.Clear();
                        botOwner.Memory.GoalEnemy = null;
                    }
                }
                else
                {
                    if (HandleFikaEventsMap.TryGetValue(ECommandPacketType.Teleport, out var action))
                    {
                        action(mcsBotPlayer, new Vector3());
                    }
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                botOwner.Mover.AllowTeleport();
                mcsBotPlayer.Teleport(botOwner.GetMcsBotData().LeadPlayer.Position, true);
                botOwner.TalkMsg(EPhraseTrigger.Regroup);
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