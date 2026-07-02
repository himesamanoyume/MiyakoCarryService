using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Interfaces;
using MiyakoCarryService.Client.Models;
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
            EventMgr.Subscribe<McsLeadPlayerExtractedEvent>(HandleMcsLeadPlayerExtracted, this);
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
        private LootDataMgr LootDataMgr => MgrAccessor.Get<LootDataMgr>();

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            _gamePlayerOwner = null;
        }

        protected sealed override void OnRaidEnded()
        {
            base.OnRaidEnded();
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

        private void ForEachAliveBot(Action<Player> action)
        {
            var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(Singleton<GameWorld>.Instance.MainPlayer.ProfileId);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                action(mcsBotPlayer);
            }
        }

        private void ForEachAliveBot(Action<Player, BodyPartType> action, BodyPartType bodyPartType)
        {
            var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(Singleton<GameWorld>.Instance.MainPlayer.ProfileId);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                action(mcsBotPlayer, bodyPartType);
            }
        }

        #region Menu

        private void BuildMainCommandMenu()
        {
            if (_gamePlayerOwner == null)
            {
                return;
            }

            var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(Singleton<GameWorld>.Instance.MainPlayer.ProfileId).ToList();

            if (mcsBotPlayers.Count == 0)
            {
                return;
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

        public void OnCurrentActionChanged()
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
            actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMTHROWTARGETLOOTCOMMAND_NAME, Locales.TEAMTHROWTARGETLOOTCOMMAND_TARGETNAME, ThrowTargetLootCommandAction));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamEscortCommandMenu, mcsBotPlayers, Locales.TEAMESCORTCOMMAND_NAME, Locales.TEAMESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamAimingTypeCommandMenu, mcsBotPlayers, Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME));
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
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.THROWTARGETLOOTCOMMAND_NAME, Locales.THROWTARGETLOOTCOMMAND_TARGETNAME, ThrowTargetLootCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.OPENINVENTORYCOMMAND_NAME, Locales.OPENINVENTORYCOMMAND_TARGETNAME, MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction, OpenInventoryCommandAction, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME, BuildAimingTypeCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.ESCORTCOMMAND_NAME, Locales.ESCORTCOMMAND_TARGETNAME, BuildEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.PROXYCOMMAND_NAME, Locales.PROXYCOMMAND_TARGETNAME, BuildProxyCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.FORCETELEPORTCOMMAND_NAME, Locales.FORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction, mcsBotPlayer));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamQuestEscortCommandMenu, mcsBotPlayers, Locales.TEAMQUESTESCORTCOMMAND_NAME, Locales.TEAMQUESTESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamTransitEscortCommandMenu, mcsBotPlayers, Locales.TEAMTRANSITESCORTCOMMAND_NAME, Locales.TEAMTRANSITESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamExfilEscortCommandMenu, mcsBotPlayers, Locales.TEAMEXFILESCORTCOMMAND_NAME, Locales.TEAMEXFILESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamSwitchEscortCommandMenu, mcsBotPlayers, Locales.TEAMSWITCHESCORTCOMMAND_NAME, Locales.TEAMSWITCHESCORTCOMMAND_TARGETNAME));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.QUESTESCORTCOMMAND_NAME, Locales.QUESTESCORTCOMMAND_TARGETNAME, BuildQuestEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.TRANSITESCORTCOMMAND_NAME, Locales.TRANSITESCORTCOMMAND_TARGETNAME, BuildTransitEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.EXFILESCORTCOMMAND_NAME, Locales.EXFILESCORTCOMMAND_TARGETNAME, BuildExfilEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.SWITCHESCORTCOMMAND_NAME, Locales.SWITCHESCORTCOMMAND_TARGETNAME, BuildSwitchEscortCommandMenu, mcsBotPlayer));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamAimingTypeCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var head = Tools.GetBodyPartTypeLocales(BodyPartType.head).McsLocalized();
            actionsReturnClass.Actions.Add(MakeTeamCommand(head, head, ChangeAimingBodyPartCommandAction, BodyPartType.head));

            var body = Tools.GetBodyPartTypeLocales(BodyPartType.body).McsLocalized();
            actionsReturnClass.Actions.Add(MakeTeamCommand(body, body, ChangeAimingBodyPartCommandAction, BodyPartType.body));

            var leftArm = Tools.GetBodyPartTypeLocales(BodyPartType.leftArm).McsLocalized();
            actionsReturnClass.Actions.Add(MakeTeamCommand(leftArm, leftArm, ChangeAimingBodyPartCommandAction, BodyPartType.leftArm));

            var rightArm = Tools.GetBodyPartTypeLocales(BodyPartType.rightArm).McsLocalized();
            actionsReturnClass.Actions.Add(MakeTeamCommand(rightArm, rightArm, ChangeAimingBodyPartCommandAction, BodyPartType.rightArm));

            var leftLeg = Tools.GetBodyPartTypeLocales(BodyPartType.leftLeg).McsLocalized();
            actionsReturnClass.Actions.Add(MakeTeamCommand(leftLeg, leftLeg, ChangeAimingBodyPartCommandAction, BodyPartType.leftLeg));

            var rightLeg = Tools.GetBodyPartTypeLocales(BodyPartType.rightLeg).McsLocalized();
            actionsReturnClass.Actions.Add(MakeTeamCommand(rightLeg, rightLeg, ChangeAimingBodyPartCommandAction, BodyPartType.rightLeg));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildAimingTypeCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var head = Tools.GetBodyPartTypeLocales(BodyPartType.head).McsLocalized();
            actionsReturnClass.Actions.Add(MakeMemberCommand(head, head, false, ChangeAimingBodyPartCommandAction, mcsBotPlayer, BodyPartType.head));

            var body = Tools.GetBodyPartTypeLocales(BodyPartType.body).McsLocalized();
            actionsReturnClass.Actions.Add(MakeMemberCommand(body, body, false, ChangeAimingBodyPartCommandAction, mcsBotPlayer, BodyPartType.body));

            var leftArm = Tools.GetBodyPartTypeLocales(BodyPartType.leftArm).McsLocalized();
            actionsReturnClass.Actions.Add(MakeMemberCommand(leftArm, leftArm, false, ChangeAimingBodyPartCommandAction, mcsBotPlayer, BodyPartType.leftArm));

            var rightArm = Tools.GetBodyPartTypeLocales(BodyPartType.rightArm).McsLocalized();
            actionsReturnClass.Actions.Add(MakeMemberCommand(rightArm, rightArm, false, ChangeAimingBodyPartCommandAction, mcsBotPlayer, BodyPartType.rightArm));

            var leftLeg = Tools.GetBodyPartTypeLocales(BodyPartType.leftLeg).McsLocalized();
            actionsReturnClass.Actions.Add(MakeMemberCommand(leftLeg, leftLeg, false, ChangeAimingBodyPartCommandAction, mcsBotPlayer, BodyPartType.leftLeg));

            var rightLeg = Tools.GetBodyPartTypeLocales(BodyPartType.rightLeg).McsLocalized();
            actionsReturnClass.Actions.Add(MakeMemberCommand(rightLeg, rightLeg, false, ChangeAimingBodyPartCommandAction, mcsBotPlayer, BodyPartType.rightLeg));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamTransitEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var transitDatas = _gameloop.GetDatas<TransitData, TransitDataMgr>();
            foreach (var transitData in transitDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayers, transitData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamExfilEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var exfilDatas = _gameloop.GetDatas<ExfilData, ExfilDataMgr>();
            foreach (var exfilData in exfilDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayers, exfilData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamSwitchEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var switchDatas = _gameloop.GetDatas<SwitchData, SwitchDataMgr>();
            foreach (var switchData in switchDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayers, switchData));
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
                actionsReturnClass.Actions.Add(EscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayer, transitData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildExfilEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var exfilDatas = _gameloop.GetDatas<ExfilData, ExfilDataMgr>();
            foreach (var exfilData in exfilDatas)
            {
                actionsReturnClass.Actions.Add(EscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayer, exfilData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildSwitchEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var switchDatas = _gameloop.GetDatas<SwitchData, SwitchDataMgr>();
            foreach (var switchData in switchDatas)
            {
                actionsReturnClass.Actions.Add(EscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayer, switchData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamSubQuestEscortCommandMenu(List<QuestData> questDatas, List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            foreach (var questData in questDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayers, questData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildSubQuestEscortCommandMenu(List<QuestData> questDatas, Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            foreach (var questData in questDatas)
            {
                actionsReturnClass.Actions.Add(EscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayer, questData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildProxyCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.QUESTPROXYCOMMAND_NAME, Locales.QUESTPROXYCOMMAND_TARGETNAME, BuildQuestProxyCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.SWITCHPROXYCOMMAND_NAME, Locales.SWITCHPROXYCOMMAND_TARGETNAME, BuildSwitchProxyCommandMenu, mcsBotPlayer));

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildQuestProxyCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var questDataMgr = MgrAccessor.Get<QuestDataMgr>();
            if (questDataMgr != null)
            {
                var questDataGroup = questDataMgr.GetQuestDataByGroup();
                foreach ((var questDataClass, var questDatas) in questDataGroup)
                {
                    actionsReturnClass.Actions.Add(MakeCommand(questDataClass.Template.Name.McsLocalized(), Locales.SUBQUESTPROXYCOMMAND_TARGETNAME, false, () => BuildSubQuestProxyCommandMenu(questDatas, mcsBotPlayer)));
                }
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildSwitchProxyCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var switchDatas = _gameloop.GetDatas<SwitchData, SwitchDataMgr>();
            foreach (var switchData in switchDatas)
            {
                actionsReturnClass.Actions.Add(ExcuteCommonProxyActionCommand(InteractionProxyActionCommandAction, mcsBotPlayer, switchData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildSubQuestProxyCommandMenu(List<QuestData> questDatas, Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            foreach (var questData in questDatas)
            {
                actionsReturnClass.Actions.Add(ExcuteQuestProxyActionCommand(QuestProxyActionCommandAction, mcsBotPlayer, questData));
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

        private ActionsTypesClass MakeMemberCommand(string name, string targetName, bool disabled, Action<Player, BodyPartType> action, Player mcsBotPlayer, BodyPartType bodyPartType)
        {
            return MakeCommand(name, targetName, disabled, () => action(mcsBotPlayer, bodyPartType));
        }

        private ActionsTypesClass MakeMemberCommand(string name, string targetName, bool disabled, Action<Player> action, Player mcsBotPlayer)
        {
            return MakeCommand(name, targetName, disabled, () => action(mcsBotPlayer));
        }

        private ActionsTypesClass MakeTeamCommand(string name, string targetName, Action<Player> action)
        {
            return MakeCommand(name, targetName, false, () => ForEachAliveBot(action));
        }

        private ActionsTypesClass MakeTeamCommand(string name, string targetName, Action<Player, BodyPartType> action, BodyPartType bodyPartType)
        {
            return MakeCommand(name, targetName, false, () => ForEachAliveBot(action, bodyPartType));
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

        private ActionsTypesClass TeamEscortToWorldPosCommand(Action<Player, WorldData> action, List<Player> mcsBotPlayers, WorldData worldData)
        {
            return new ActionsTypesClass
            {
                Name = worldData.GetActionName(),
                TargetName = worldData.GetActionTargetName(Singleton<GameWorld>.Instance.MainPlayer.Position),
                Disabled = worldData.IsDisabled(),
                Action = () =>
                {
                    foreach (var mcsBotPlayer in mcsBotPlayers)
                    {
                        if (mcsBotPlayer == null || !mcsBotPlayer.HealthController.IsAlive)
                        {
                            continue;
                        }
                        action(mcsBotPlayer, worldData);
                    }
                }
            };
        }

        private ActionsTypesClass EscortToWorldPosCommand(Action<Player, WorldData> action, Player mcsBotPlayer, WorldData worldData)
        {
            return new ActionsTypesClass
            {
                Name = worldData.GetActionName(),
                TargetName = worldData.GetActionTargetName(Singleton<GameWorld>.Instance.MainPlayer.Position),
                Disabled = worldData.IsDisabled(),
                Action = () => action(mcsBotPlayer, worldData)
            };
        }

        private ActionsTypesClass ExcuteQuestProxyActionCommand(Action<Player, QuestData> action, Player mcsBotPlayer, QuestData questData)
        {
            return new ActionsTypesClass
            {
                Name = questData.GetActionName(),
                TargetName = questData.GetActionTargetName(Singleton<GameWorld>.Instance.MainPlayer.Position),
                Disabled = questData.IsProxyActionDisabled(),
                Action = () => action(mcsBotPlayer, questData)
            };
        }

        private ActionsTypesClass ExcuteCommonProxyActionCommand(Action<Player, IProxyActor> action, Player mcsBotPlayer, IProxyActor interactiveObjectData)
        {
            return new ActionsTypesClass
            {
                Name = interactiveObjectData.GetActionName(),
                TargetName = interactiveObjectData.GetActionTargetName(Singleton<GameWorld>.Instance.MainPlayer.Position),
                Disabled = interactiveObjectData.IsProxyActionDisabled(),
                Action = () => action(mcsBotPlayer, interactiveObjectData)
            };
        }

        #region Action

        private void EscortToWorldPosCommandAction(Player mcsBotPlayer, WorldData worldData)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.Escort,
                    Position = worldData.GetPos()
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
                    mcsBotPlayerData.TargetPos = worldData.GetPos();
                    mcsBotPlayerData.IsLooting = false;
                }
            }
            CloseCommandMenuAction();
        }

        private void ReportAboutEnemyCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
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
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
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
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
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
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
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
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
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
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
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
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
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
                            mcsBotPlayerData.TargetPos = pos.Value;
                            mcsBotPlayerData.ProxyTargetId = null;
                        }
                        botOwner.Mover.LastTimePosChanged = Time.time;
                        botOwner.StopMove();
                    }
                }
            }
            CloseCommandMenuAction();
        }

        private void ChangeAimingBodyPartCommandAction(Player mcsBotPlayer, BodyPartType aimingBodyPartType)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.AimingBodyPart,
                    Position = null,
                    AimingBodyPartType = aimingBodyPartType
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.AimingBodyPartType = aimingBodyPartType;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });
            }
            CloseCommandMenuAction();
        }

        private void HoldPositionCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
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
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.HoldPosition,
                });
            }
            CloseCommandMenuAction();
        }

        private void ThrowTargetLootCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.ThrowTargetLoot
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                if (botOwner.ExternalItemsController.HaveItemsToDrop())
                {
                    botOwner.StopMove();
                    var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                    if (mcsBotPlayerData != null)
                    {
                        mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldThrowTargetLoot);
                        mcsBotPlayerData.IsLooting = false;
                        mcsBotPlayerData.TargetPos = null;
                        mcsBotPlayerData.ProxyTargetId = null;
                    }
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Roger,
                    });
                }
                else
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Negative,
                    });
                }
            }
            CloseCommandMenuAction();
        }

        private void ForceTeleportCommandAction(Player mcsBotPlayer)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
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
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
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

        private void HandleMcsLeadPlayerExtracted(McsLeadPlayerExtractedEvent @event)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(@event.McsLeadPlayerId);
                foreach (var mcsBotPlayer in mcsBotPlayers)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.GoToExfil,
                        Position = null
                    });
                }
            }
            else
            {
                var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(@event.McsLeadPlayerId);
                foreach (var mcsBotPlayer in mcsBotPlayers)
                {
                    if (mcsBotPlayer == null)
                    {
                        continue;
                    }

                    var mcsBotPlayerData = mcsBotPlayer.AIData.BotOwner.GetMcsBotPlayerData();
                    if (mcsBotPlayerData == null)
                    {
                        continue;
                    }
                    mcsBotPlayerData.SetDecision(null, EDecision.ShouldExfil);
                }
            }
        }

        public void InteractionProxyActionCommandAction(Player mcsBotPlayer, IProxyActor proxyAction)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.InteractionProxyAction,
                    TargetId = proxyAction.Id()
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
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldInteractionProxyAction);
                    var interactableObjectData = Singleton<GameWorld>.Instance.FindInteractableObjectData(proxyAction.Id());
                    if (interactableObjectData != null)
                    {
                        mcsBotPlayerData.ProxyTargetId = proxyAction.Id();
                        mcsBotPlayerData.TargetPos = interactableObjectData.GetPos();
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Roger,
                        });
                    }
                    mcsBotPlayerData.IsLooting = false;
                }
            }
            CloseCommandMenuAction();
        }

        private void QuestProxyActionCommandAction(Player mcsBotPlayer, QuestData questData)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.QuestProxyAction,
                    Position = questData.GetPos(),
                    TargetId = questData.Id()
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
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldQuestProxyAction);
                    mcsBotPlayerData.ProxyTargetId = questData.Id();
                    mcsBotPlayerData.TargetPos = questData.GetPos();
                    mcsBotPlayerData.IsLooting = false;
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Roger,
                    });
                }
            }
            CloseCommandMenuAction();
        }

        public void LootProxyActionCommandAction(Player mcsBotPlayer, LootData lootData)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = ECommandPacketType.LootProxyAction,
                    Position = lootData.RootTransform.position,
                    TargetId = lootData.Item.Id
                });
            }
            else
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (botOwner.Memory.HaveEnemy)
                {
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Negative,
                    });
                    if (mcsBotPlayerData != null)
                    {
                        mcsBotPlayerData.ProxyTargetId = null;
                        mcsBotPlayerData.TargetPos = null;
                    }
                }
                botOwner.Mover.LastTimePosChanged = Time.time;
                botOwner.StopMove();
                
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.SetDecision([EDecision.ShouldRegroup], EDecision.ShouldLootProxyAction);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.ProxyTargetId = lootData.Item.Id;
                    mcsBotPlayerData.TargetPos = lootData.RootTransform.position;
                    LootDataMgr.UnlockLootingTarget(lootData);
                    LootDataMgr.UnlockLootingTargetRootTransform(lootData.RootTransform);
                    if (!LootDataMgr.IsLockedLootingTarget(lootData) && !LootDataMgr.IsLockedLootingTargetRootTransform(lootData.RootTransform))
                    {
                        LootDataMgr.LockLootItemToTarget(lootData);
                        LootDataMgr.LockLootingTargetRootTransform(lootData.RootTransform);
                        mcsBotPlayerData.LootingTarget = lootData;
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Roger,
                        });
                    }
                    else
                    {
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Negative,
                        });
                        mcsBotPlayerData.RemoveDecision(EDecision.ShouldLootProxyAction);
                        mcsBotPlayerData.ProxyTargetId = null;
                        mcsBotPlayerData.TargetPos = null;
                    }
                }
            }
            CloseCommandMenuAction();
        }

        #endregion
    }
}