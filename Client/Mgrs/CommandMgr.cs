using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Patches.Events;
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
                }
            );
        }

        private void ForEachAliveBot(Action<Player> action)
        {
            foreach (var id in _mySquadMcsBotPlayerIds)
            {
                var mcsBotPlayer = TryGetMcsBotPlayer(id);
                if (mcsBotPlayer == null || !mcsBotPlayer.HealthController.IsAlive)
                {
                    continue;
                }
                action(mcsBotPlayer);
            }
        }

        #region Menu

        private void BuildMainCommandMenu()
        {
            if (_gamePlayerOwner == null || _mySquadMcsBotPlayerIds.Count == 0)
            {
                return;
            }

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

            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamCommandMenu, mcsBotPlayers));

            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                actionsReturnClass.Actions.Add(MakeMemberCommand(BuildMemberCommandMenu, mcsBotPlayer));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void PreBuildCommandMenu(out ActionsReturnClass actionsReturnClass)
        {
            actionsReturnClass = new ActionsReturnClass
            {
                Actions = new()
            };

            actionsReturnClass.CurrentActionChanged.Bind(OnCurrentActionChanged);
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

        private void PostBuildCommandMenu(ActionsReturnClass actionsReturnClass)
        {
            if (actionsReturnClass != null)
            {
                actionsReturnClass.Actions.Add(MakeCommand(Locales.CANCELCOMMAND_NAME, Locales.CANCELCOMMAND_TARGETNAME, false, CloseCommandMenuAction));
                actionsReturnClass.InitSelected();
            }

            if (_gamePlayerOwner == null)
            {
                return;
            }

            _gamePlayerOwner.AvailableInteractionState.Value = actionsReturnClass;
        }

        private void CloseCommandMenuAction()
        {
            _gamePlayerOwner.AvailableInteractionState.Value = new ActionsReturnClass();
        }

        private void BuildTeamCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMREPORTABOUTENEMYCOMMAND_NAME, Locales.TEAMREPORTABOUTENEMYCOMMAND_TARGETNAME, ReportAboutEnemyCommandAction));
            actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMONYOUROWNCOMMAND_NAME, Locales.TEAMONYOUROWNCOMMAND_TARGETNAME, OnYourOwnCommandAction));
            actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMREGROUPCOMMAND_NAME, Locales.TEAMREGROUPCOMMAND_TARGETNAME, RegroupCommandAction));
            actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMGOTOPOINTCOMMAND_NAME, Locales.TEAMGOTOPOINTCOMMAND_TARGETNAME, GoToPointCommandAction));
            actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMHOLDPOSITIONCOMMAND_NAME, Locales.TEAMHOLDPOSITIONCOMMAND_TARGETNAME, HoldPositionCommandAction));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamEscortCommandMenu, mcsBotPlayers, Locales.TEAMESCORTCOMMAND_NAME, Locales.TEAMESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMFORCETELEPORTCOMMAND_NAME, Locales.TEAMFORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildMemberCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.REPORTABOUTENEMYCOMMAND_NAME, Locales.REPORTABOUTENEMYCOMMAND_TARGETNAME, ReportAboutEnemyCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.ONYOUROWNCOMMAND_NAME, Locales.ONYOUROWNCOMMAND_TARGETNAME, OnYourOwnCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.REGROUPCOMMAND_NAME, Locales.REGROUPCOMMAND_TARGETNAME, RegroupCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.GOTOPOINTCOMMAND_NAME, Locales.GOTOPOINTCOMMAND_TARGETNAME, GoToPointCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.HOLDPOSITIONCOMMAND_NAME, Locales.HOLDPOSITIONCOMMAND_TARGETNAME, HoldPositionCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.OPENINVENTORYCOMMAND_NAME, Locales.OPENINVENTORYCOMMAND_TARGETNAME, OpenInventoryCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.ESCORTCOMMAND_NAME, Locales.TEAMESCORTCOMMAND_TARGETNAME, BuildEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.FORCETELEPORTCOMMAND_NAME, Locales.FORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction, mcsBotPlayer));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamQuestEscortCommandMenu, mcsBotPlayers, Locales.TEAMQUESTESCORTCOMMAND_NAME, Locales.TEAMQUESTESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamTransitEscortCommandMenu, mcsBotPlayers, Locales.TEAMTRANSITESCORTCOMMAND_NAME, Locales.TEAMTRANSITESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamExfilEscortCommandMenu, mcsBotPlayers, Locales.TEAMEXFILESCORTCOMMAND_NAME, Locales.TEAMEXFILESCORTCOMMAND_TARGETNAME));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.QUESTESCORTCOMMAND_NAME, Locales.QUESTESCORTCOMMAND_TARGETNAME, BuildQuestEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.TRANSITESCORTCOMMAND_NAME, Locales.TRANSITESCORTCOMMAND_TARGETNAME, BuildTransitEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.EXFILESCORTCOMMAND_NAME, Locales.EXFILESCORTCOMMAND_TARGETNAME, BuildExfilEscortCommandMenu, mcsBotPlayer));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamTransitEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var transitDatas = _gameloop.GetDatas<TransitData, TransitDataMgr>();
            foreach (var transitData in transitDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToTriggerPosCommand(EscortToTriggerPosCommandAction, mcsBotPlayers, transitData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamExfilEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var exfilDatas = _gameloop.GetDatas<ExfilData, ExfilDataMgr>();
            foreach (var exfilData in exfilDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToTriggerPosCommand(EscortToTriggerPosCommandAction, mcsBotPlayers, exfilData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamQuestEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var questDataMgr = MgrAccessor.Get<QuestDataMgr>();
            if (questDataMgr != null)
            {
                var questDataGroup = questDataMgr.GetQuestDataByGroup();
                foreach ((var questDataClass, var questDatas) in questDataGroup)
                {
                    actionsReturnClass.Actions.Add(MakeCommand(questDataClass.Template.Name.McsLocalized(), Locales.SUBQUESTESCORTCOMMAND_TARGETNAME, false, () => BuildTeamSubQuestEscortCommandMenu(questDatas, mcsBotPlayers)));
                }
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildQuestEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var questDataMgr = MgrAccessor.Get<QuestDataMgr>();
            if (questDataMgr != null)
            {
                var questDataGroup = questDataMgr.GetQuestDataByGroup();
                foreach ((var questDataClass, var questDatas) in questDataGroup)
                {
                    actionsReturnClass.Actions.Add(MakeCommand(questDataClass.Template.Name.McsLocalized(), Locales.SUBQUESTESCORTCOMMAND_TARGETNAME, false, () => BuildSubQuestEscortCommandMenu(questDatas, mcsBotPlayer)));
                }
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTransitEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var transitDatas = _gameloop.GetDatas<TransitData, TransitDataMgr>();
            foreach (var transitData in transitDatas)
            {
                actionsReturnClass.Actions.Add(EscortToTriggerPosCommand(EscortToTriggerPosCommandAction, mcsBotPlayer, transitData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildExfilEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var exfilDatas = _gameloop.GetDatas<ExfilData, ExfilDataMgr>();
            foreach (var exfilData in exfilDatas)
            {
                actionsReturnClass.Actions.Add(EscortToTriggerPosCommand(EscortToTriggerPosCommandAction, mcsBotPlayer, exfilData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamSubQuestEscortCommandMenu(List<QuestData> questDatas, List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            foreach (var questData in questDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToTriggerPosCommand(EscortToTriggerPosCommandAction, mcsBotPlayers, questData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildSubQuestEscortCommandMenu(List<QuestData> questDatas, Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            foreach (var questData in questDatas)
            {
                actionsReturnClass.Actions.Add(EscortToTriggerPosCommand(EscortToTriggerPosCommandAction, mcsBotPlayer, questData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        #endregion

        private static ActionsTypesClass MakeCommand(string name, string targetName, bool disabled, Action action)
        {
            return new ActionsTypesClass { Name = name, TargetName = targetName, Disabled = disabled, Action = action };
        }

        private static ActionsTypesClass MakeCommand(string name, string targetName, bool disabled, Action<List<Player>> action, List<Player> mcsBotPlayers)
        {
            return new ActionsTypesClass { Name = name, TargetName = targetName, Disabled = disabled, Action = () => action(mcsBotPlayers) };
        }

        private ActionsTypesClass MakeMemberCommand(string name, string targetName, Action<Player> action, Player mcsBotPlayer)
        {
            return MakeCommand(name, targetName, false, () => action(mcsBotPlayer));
        }

        private ActionsTypesClass MakeMemberCommand(string name, string targetName, bool disabled, Action<Player> action, Player mcsBotPlayer)
        {
            return MakeCommand(name, targetName, disabled, () => action(mcsBotPlayer));
        }

        private ActionsTypesClass MakeTeamCommand(string name, string targetName, Action<Player> action)
        {
            return MakeCommand(name, targetName, false, () => ForEachAliveBot(action));
        }

        private ActionsTypesClass TeamCommandSubMenu(Action<List<Player>> action, List<Player> mcsBotPlayers)
        {
            return TeamCommandSubMenu(action, mcsBotPlayers, Locales.TEAMCOMMAND_NAME, Locales.TEAMCOMMAND_TARGETNAME);
        }

        private ActionsTypesClass TeamCommandSubMenu(Action<List<Player>> action, List<Player> mcsBotPlayers, string name, string targetName)
        {
            return MakeCommand(name, targetName, mcsBotPlayers.All(p => !p.HealthController.IsAlive), action, mcsBotPlayers);
        }

        private ActionsTypesClass MakeMemberCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return MakeMemberCommand(mcsBotPlayer.Profile.Info.Nickname, Locales.MEMBERCOMMAND_TARGETNAME, !mcsBotPlayer.HealthController.IsAlive, action, mcsBotPlayer);
        }

        private ActionsTypesClass TeamEscortToTriggerPosCommand(Action<Player, TriggerData> action, List<Player> mcsBotPlayers, TriggerData triggerData)
        {
            return new ActionsTypesClass
            {
                Name = triggerData.GetActionName(),
                TargetName = triggerData.GetActionTargetName(Singleton<GameWorld>.Instance.MainPlayer.Position),
                Disabled = triggerData.IsDisabled(),
                Action = () =>
                {
                    foreach (var mcsBotPlayer in mcsBotPlayers)
                    {
                        if (mcsBotPlayer == null || !mcsBotPlayer.HealthController.IsAlive)
                        {
                            continue;
                        }
                        action(mcsBotPlayer, triggerData);
                    }
                }
            };
        }

        private ActionsTypesClass EscortToTriggerPosCommand(Action<Player, TriggerData> action, Player mcsBotPlayer, TriggerData triggerData)
        {
            return new ActionsTypesClass
            {
                Name = triggerData.GetActionName(),
                TargetName = triggerData.GetActionTargetName(Singleton<GameWorld>.Instance.MainPlayer.Position),
                Disabled = triggerData.IsDisabled(),
                Action = () => action(mcsBotPlayer, triggerData)
            };
        }

        #region Action

        private void EscortToTriggerPosCommandAction(Player mcsBotPlayer, TriggerData triggerData)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.Escort,
                    Position = triggerData.GetPos()
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                if (botOwner.Memory.HaveEnemy)
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Negative,
                    });
                }
                botOwner.Mover.LastTimePosChanged = Time.time;
                botOwner.StopMove();
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldEscort);
                    mcsBotPlayerData.EscortPos = triggerData.GetPos();
                    mcsBotPlayerData.IsLooting = false;
                }
            }
            CloseCommandMenuAction();
        }

        private void ReportAboutEnemyCommandAction(Player mcsBotPlayer)
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
                    mcsBotPlayerData.EscortPos = null;
                }
                if (botOwner.Memory.HaveEnemy)
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.OnFirstContact,
                        Position = botOwner.Memory.GoalEnemy.EnemyLastPosition
                    });
                }
                else
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Clear,
                    });
                }
            }
            CloseCommandMenuAction();
        }

        private void OnYourOwnCommandAction(Player mcsBotPlayer)
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
                    mcsBotPlayerData.SetDecision();
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.EscortPos = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });
            }
            CloseCommandMenuAction();
        }

        private void RegroupCommandAction(Player mcsBotPlayer)
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
                    mcsBotPlayerData.SetDecision(null, EDecision.ShouldRegroup);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.EscortPos = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Regroup,
                });
            }
            CloseCommandMenuAction();
        }

        private void GoToPointCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !McsMgr.IsHost)
            {
                if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask))
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
                if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    var pos = Tools.GetPosNearTarget(raycastHit.point, botOwner);
                    if (pos.HasValue)
                    {
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Going,
                        });

                        var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                        if (mcsBotPlayerData != null)
                        {
                            mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldGoToPoint);
                            mcsBotPlayerData.IsLooting = false;
                            mcsBotPlayerData.EscortPos = null;
                        }
                        botOwner.Mover.LastTimePosChanged = Time.time;
                        botOwner.StopMove();
                        botOwner.GoToSomePointData.SetPoint(pos.Value);
                    }
                }
            }
            CloseCommandMenuAction();
        }

        private void HoldPositionCommandAction(Player mcsBotPlayer)
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
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldHoldPosition);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.EscortPos = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.HoldPosition,
                });
            }
            CloseCommandMenuAction();
        }

        private void ForceTeleportCommandAction(Player mcsBotPlayer)
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
                    mcsBotPlayerData.SetDecision();
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.EscortPos = null;
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

        private void OpenInventoryCommandAction(Player mcsBotPlayer)
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

        #endregion
    }
}