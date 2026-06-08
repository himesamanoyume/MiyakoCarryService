
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Patches.Events;
using MiyakoCarryService.Client.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

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
                return field ??= Singleton<GameWorld>.Instance.MainPlayer.GetGamePlayerOwner();
            }
        }

        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        private List<MongoID> _mySquadMcsBotPlayerIds = new();
        private ConcurrentDictionary<MongoID, Player> _mySquadMcsBotPlayers = new();

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            TasksExtensions.HandleExceptions(GetMySquadMcsBotPlayerIds());
            _gamePlayerOwner = null;
        }

        private async Task GetMySquadMcsBotPlayerIds()
        {
            _mySquadMcsBotPlayerIds = await McsRequestHandler.GetMySquadMcsBotPlayerIds(new()
            {
                Side = MatchmakerAcceptScreenShowPatch.CurrentType
            });
        }

        protected sealed override void OnRaidEnded()
        {
            base.OnRaidEnded();
            _mySquadMcsBotPlayerIds.Clear();
            _mySquadMcsBotPlayers.Clear();
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
            return _mySquadMcsBotPlayers.AddOrUpdate(
                mcsBotPlayerId,
                id =>
                {
                    var mcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(id);
                    if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost && mcsBotPlayer != null)
                    {
                        mcsBotPlayer.Profile.Info.GroupId = "Fika";
                        mcsBotPlayer.Profile.Info.TeamId = "Fika";
                    }
                    return mcsBotPlayer;
                },
                (id, oldPlayer) =>
                {
                    if (oldPlayer != null)
                    {
                        return oldPlayer;
                    }
                    var mcsBotPlayer = Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(id);
                    if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost && mcsBotPlayer != null)
                    {
                        mcsBotPlayer.Profile.Info.GroupId = "Fika";
                        mcsBotPlayer.Profile.Info.TeamId = "Fika";
                    }
                    return mcsBotPlayer;
                });
        }

        public void BuildMainCommandMenu()
        {
            if (_gamePlayerOwner == null || _mySquadMcsBotPlayerIds.Count == 0)
            {
                return;
            }

            var actionsReturnClass = new ActionsReturnClass
            {
                Actions = new()
            };

            var mcsBotPlayers = new List<Player>();
            foreach (var mcsBotPlayerId in _mySquadMcsBotPlayerIds)
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
            if (_gamePlayerOwner == null || _mySquadMcsBotPlayerIds.Count == 0)
            {
                return;
            }

            var actionsReturnClass = new ActionsReturnClass
            {
                Actions = new()
            };

            actionsReturnClass.CurrentActionChanged.Bind(OnCurrentActionChanged);

            actionsReturnClass.Actions.Add(TeamReportAboutEnemyCommand(ReportAboutEnemyCommandAction));
            actionsReturnClass.Actions.Add(TeamOnYourOwnCommand(OnYourOwnCommandAction));
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
                Action = new Action(() =>
                {
                    action(mcsBotPlayer);
                })
            };
        }

        public ActionsTypesClass TeamReportAboutEnemyCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = Locales.TEAMREPORTABOUTENEMYCOMMAND_NAME,
                TargetName = Locales.TEAMREPORTABOUTENEMYCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    foreach (var mcsBotPlayerId in _mySquadMcsBotPlayerIds)
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

        public ActionsTypesClass TeamRegroupCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = Locales.TEAMREGROUPCOMMAND_NAME,
                TargetName = Locales.TEAMREGROUPCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    foreach (var mcsBotPlayerId in _mySquadMcsBotPlayerIds)
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

        public ActionsTypesClass TeamOnYourOwnCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = Locales.TEAMREGROUPCOMMAND_NAME,
                TargetName = Locales.TEAMREGROUPCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    foreach (var mcsBotPlayerId in _mySquadMcsBotPlayerIds)
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
                Name = Locales.TEAMGOTOPOINTCOMMAND_NAME,
                TargetName = Locales.TEAMGOTOPOINTCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    foreach (var mcsBotPlayerId in _mySquadMcsBotPlayerIds)
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
                Name = Locales.TEAMHOLDPOSITIONCOMMAND_NAME,
                TargetName = Locales.TEAMHOLDPOSITIONCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    foreach (var mcsBotPlayerId in _mySquadMcsBotPlayerIds)
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

        public void ReportAboutEnemyCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.ReportAboutEnemy
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.IsLooting = false;
                }
                if (botOwner.Memory.HaveEnemy)
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.StartHeal,
                        Position = botOwner.Memory.GoalEnemy.EnemyLastPosition
                    });
                }
            }
            CloseCommandMenuAction();
        }

        public void OnYourOwnCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.OnYourOwn,
                    Position = null
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.ShouldRegroup = false;
                    mcsBotPlayerData.ShouldGoToPoint = false;
                    mcsBotPlayerData.ShouldHoldPosition = false;
                    mcsBotPlayerData.IsLooting = false;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });
            }
            CloseCommandMenuAction();
        }

        public void RegroupCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.Regroup,
                    Position = null
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.ShouldRegroup = true;
                    mcsBotPlayerData.ShouldGoToPoint = false;
                    mcsBotPlayerData.ShouldHoldPosition = false;
                    mcsBotPlayerData.IsLooting = false;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Regroup,
                });
            }
            CloseCommandMenuAction();
        }

        public void GoToPointCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, (int)AccessTools.Field(typeof(GameWorld), "int_2").GetValue(Singleton<GameWorld>.Instance)))
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.GoToPoint,
                        Position = raycastHit.point
                    });
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, (int)AccessTools.Field(typeof(GameWorld), "int_2").GetValue(Singleton<GameWorld>.Instance)))
                {
                    Vector3? validPosition = null;
                    var xOffset = GClass856.Random(1f, 4f) * GClass856.RandomSing();
                    var zOffset = GClass856.Random(1f, 4f) * GClass856.RandomSing();
                    var newPos = raycastHit.point + new Vector3(xOffset, 0f, zOffset);

                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        if (Tools.BetterDestination(3f, newPos, out var targetPos))
                        {
                            if (Mathf.Abs(targetPos.y - raycastHit.point.y) <= 2f)
                            {
                                validPosition = targetPos;
                                break;
                            }
                        }
                    }

                    if (validPosition == null && NavMesh.SamplePosition(newPos, out var navMeshHit2, 3f, -1))
                    {
                        validPosition = navMeshHit2.position;
                    }

                    if (validPosition.HasValue)
                    {
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Going,
                        });

                        var leadDir = botOwner.Position - validPosition.Value;
                        leadDir.y = 0;
                        leadDir = leadDir.normalized * 2f;

                        if (NavMesh.Raycast(validPosition.Value, leadDir + validPosition.Value, out var rayHit, -1))
                        {
                            validPosition = rayHit.position;
                        }
                        else
                        {
                            validPosition = leadDir + validPosition.Value;
                        }
                        var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                        if (mcsBotPlayerData != null)
                        {
                            mcsBotPlayerData.ShouldGoToPoint = true;
                            mcsBotPlayerData.IsLooting = false;
                        }
                        botOwner.Mover.LastTimePosChanged = Time.time;
                        botOwner.StopMove();
                        botOwner.GoToSomePointData.SetPoint(validPosition.Value);
                    }
                }
            }
            CloseCommandMenuAction();
        }

        public void HoldPositionCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.HoldPosition,
                    Position = null
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.ShouldHoldPosition = true;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.HoldPosition,
                });
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
                    foreach (var mcsBotPlayerId in _mySquadMcsBotPlayerIds)
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
            if (_gamePlayerOwner == null || _mySquadMcsBotPlayerIds.Count == 0)
            {
                return;
            }

            var actionsReturnClass = new ActionsReturnClass
            {
                Actions = new()
            };

            actionsReturnClass.CurrentActionChanged.Bind(OnCurrentActionChanged);

            actionsReturnClass.Actions.Add(ReportAboutEnemyCommand(ReportAboutEnemyCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(OnYourOwnCommand(OnYourOwnCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(RegroupCommand(RegroupCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(GoToPointCommand(GoToPointCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(HoldPositionCommand(HoldPositionCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(ForceTeleportCommand(ForceTeleportCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(OpenInventoryCommand(OpenInventoryCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(CancelCommand(CloseCommandMenuAction));

            if (actionsReturnClass != null)
            {
                actionsReturnClass.InitSelected();
            }

            _gamePlayerOwner.AvailableInteractionState.Value = actionsReturnClass;
        }

        public ActionsTypesClass ReportAboutEnemyCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = Locales.REPORTABOUTENEMYCOMMAND_NAME,
                TargetName = Locales.REPORTABOUTENEMYCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    action(mcsBotPlayer);
                })
            };
        }

        public ActionsTypesClass OnYourOwnCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = Locales.ONYOUROWNCOMMAND_NAME,
                TargetName = Locales.ONYOUROWNCOMMAND_TARGETNAME,
                Disabled = false,
                Action = new Action(() =>
                {
                    action(mcsBotPlayer);
                })
            };
        }

        public ActionsTypesClass RegroupCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = Locales.REGROUPCOMMAND_NAME,
                TargetName = Locales.REGROUPCOMMAND_TARGETNAME,
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
                Name = Locales.GOTOPOINTCOMMAND_NAME,
                TargetName = Locales.GOTOPOINTCOMMAND_TARGETNAME,
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
                Name = Locales.HOLDPOSITIONCOMMAND_NAME,
                TargetName = Locales.HOLDPOSITIONCOMMAND_TARGETNAME,
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

        public ActionsTypesClass OpenInventoryCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = Locales.OPENINVENTORYCOMMAND_NAME,
                TargetName = Locales.OPENINVENTORYCOMMAND_TARGETNAME,
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
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.Teleport,
                    Position = null
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                botOwner.Mover.AllowTeleport();
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.ShouldGoToPoint = false;
                    mcsBotPlayerData.ShouldHoldPosition = false;
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayer.Teleport(mcsBotPlayerData.LeadPlayer.Position, true);
                }
                var playerPosition = mcsBotPlayer.Position;
                botOwner.Mover.LastGoodCastPoint = botOwner.Mover.PrevSuccessLinkedFrom_1 = botOwner.Mover.PrevLinkPos = botOwner.Mover.PositionOnWayInner = playerPosition;
                botOwner.Mover.LastGoodCastPointTime = Time.time;
                botOwner.Mover.PrevPosLinkedTime_1 = 0f;
                botOwner.Mover.SetPlayerToNavMesh(playerPosition);
                botOwner.Mover.RecalcWay();
                botOwner.Mover.Pause = true;
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });

                if (!MiyakoCarryServicePlugin.SAINInstalled)
                {
                    botOwner.Memory.GoalTarget.Clear();
                    botOwner.Memory.GoalEnemy = null;
                }
            }
            CloseCommandMenuAction();
        }

        public void OpenInventoryCommandAction(Player mcsBotPlayer)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var itemOwners = gameWorld.ItemOwners;
            foreach (var itemOwner in itemOwners)
            {
                var rootItem = itemOwner.Key.RootItem;
                if (!rootItem.IsPlayerInventory)
                {
                    continue;
                }

                var profileId = rootItem.Owner?.ID;
                if (string.IsNullOrEmpty(profileId))
                {
                    continue;
                }

                if (mcsBotPlayer.ProfileId == profileId && rootItem.Owner is TraderControllerClass traderControllerClass)
                {
                    var inventoryActionClass = new InventoryActionClass
                    {
                        owner = _gamePlayerOwner,
                        rootItem = rootItem,
                        lootItemOwner = traderControllerClass
                    };
                    inventoryActionClass.method_3();
                }
            }
            CloseCommandMenuAction();
        }
    }
}