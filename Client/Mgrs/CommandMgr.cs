

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
using MiyakoCarryService.Client.Extensions;
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
                return field ??= Singleton<GameWorld>.Instance.MainPlayer.GetComponentInChildren<GamePlayerOwner>();
            }
        }

        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        private List<MongoID> _mcsBotPlayerIds = new();
        private ConcurrentDictionary<MongoID, Player> _mcsBotPlayers = new();
        public Action<Player, ECommandPacketType, Vector3?> HandleFikaEvent = null;

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            _ = GetMcsBotPlayerIds();
            _gamePlayerOwner = null;
        }

        private async Task GetMcsBotPlayerIds()
        {
            _mcsBotPlayerIds = await McsRequestHandler.GetMcsBotPlayerIds(new()
            {
                Side = MatchmakerAcceptScreenShowPatch.CurrentType
            });

            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                foreach (var mcsBotPlayerId in _mcsBotPlayerIds)
                {
                    var mcsBotPlayer = TryGetMcsBotPlayer(mcsBotPlayerId);
                    if (mcsBotPlayer == null)
                    {
                        continue;
                    }
                    mcsBotPlayer.Profile.Info.GroupId = "Fika";
                    mcsBotPlayer.Profile.Info.TeamId = "Fika";
                }
            }

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
            return _mcsBotPlayers.AddOrUpdate(
                mcsBotPlayerId, 
                id => Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(id),
                (id, oldPlayer) =>
                {
                    if (oldPlayer != null)
                    {
                        return oldPlayer;
                    }
                    return Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(id);
                });
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

            actionsReturnClass.Actions.Add(TeamReportAboutEnemyCommand(ReportAboutEnemyCommandAction));
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

        public ActionsTypesClass TeamReportAboutEnemyCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = Locales.TEAMREPORTABOUTENEMYCOMMAND_NAME,
                TargetName = Locales.TEAMREPORTABOUTENEMYCOMMAND_TARGETNAME,
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

        public ActionsTypesClass TeamRegroupCommand(Action<Player> action)
        {
            return new ActionsTypesClass
            {
                Name = Locales.TEAMREGROUPCOMMAND_NAME,
                TargetName = Locales.TEAMREGROUPCOMMAND_TARGETNAME,
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
                Name = Locales.TEAMGOTOPOINTCOMMAND_NAME,
                TargetName = Locales.TEAMGOTOPOINTCOMMAND_TARGETNAME,
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
                Name = Locales.TEAMHOLDPOSITIONCOMMAND_NAME,
                TargetName = Locales.TEAMHOLDPOSITIONCOMMAND_TARGETNAME,
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

        public void ReportAboutEnemyCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                if (HandleFikaEvent != null)
                {
                    var botOwner = mcsBotPlayer.AIData.BotOwner;
                    if (botOwner.Memory.HaveEnemy)
                    {
                        HandleFikaEvent(mcsBotPlayer, ECommandPacketType.ReportAboutEnemy, botOwner.Memory.GoalEnemy.EnemyLastPosition);
                    }
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                if (botOwner.Memory.HaveEnemy)
                {
                    botOwner.TalkMsg(EPhraseTrigger.OnFirstContact, botOwner.Memory.GoalEnemy.EnemyLastPosition);
                }
            }
            CloseCommandMenuAction();
        }

        public void RegroupCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                if (HandleFikaEvent != null)
                {
                    HandleFikaEvent(mcsBotPlayer, ECommandPacketType.Regroup, new Vector3());
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.GetMcsBotPlayerData().ShouldGoToPoint = false;
                botOwner.GetMcsBotPlayerData().ShouldHoldPosition = false;
                botOwner.TalkMsg(EPhraseTrigger.Regroup);
            }
            CloseCommandMenuAction();
        }

        public void GoToPointCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                if (HandleFikaEvent != null)
                {
                    if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, (int)AccessTools.Field(typeof(GameWorld), "int_2").GetValue(Singleton<GameWorld>.Instance)))
                    {
                        HandleFikaEvent(mcsBotPlayer, ECommandPacketType.GoToPoint, raycastHit.point);
                    }
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, (int)AccessTools.Field(typeof(GameWorld), "int_2").GetValue(Singleton<GameWorld>.Instance)))
                {
                    Vector3? validPosition = null;
                    var xOffset = GClass856.Random(3f, 4f) * GClass856.RandomSing();
                    var zOffset = GClass856.Random(3f, 4f) * GClass856.RandomSing();
                    var newPos = raycastHit.point + new Vector3(xOffset, 0f, zOffset);

                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        if (NavMesh.SamplePosition(newPos, out var navMeshHit1, 7f, -1))
                        {
                            if (Mathf.Abs(navMeshHit1.position.y - raycastHit.point.y) <= 2f)
                            {
                                validPosition = navMeshHit1.position;
                                break;
                            }
                        }
                    }

                    if (validPosition == null && NavMesh.SamplePosition(newPos, out var navMeshHit2, 7f, -1))
                    {
                        validPosition = navMeshHit2.position;
                    }

                    if (validPosition.HasValue)
                    {
                        botOwner.TalkMsg(EPhraseTrigger.Going);
                        botOwner.GetMcsBotPlayerData().ShouldGoToPoint = true;
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
                if (HandleFikaEvent != null)
                {
                    HandleFikaEvent(mcsBotPlayer, ECommandPacketType.HoldPosition, new Vector3());
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                botOwner.GetMcsBotPlayerData().ShouldHoldPosition = true;
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
                Disabled = MiyakoCarryServicePlugin.SAINInstalled,
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

            actionsReturnClass.Actions.Add(ReportAboutEnemyCommand(ReportAboutEnemyCommandAction, mcsBotPlayer));
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
                Disabled = MiyakoCarryServicePlugin.SAINInstalled,
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
                if (HandleFikaEvent != null)
                {
                    HandleFikaEvent(mcsBotPlayer, ECommandPacketType.Teleport, new Vector3());
                }
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                botOwner.Mover.AllowTeleport();
                mcsBotPlayer.Teleport(botOwner.GetMcsBotPlayerData().LeadPlayer.Position, true);
                botOwner.TalkMsg(EPhraseTrigger.Roger);
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