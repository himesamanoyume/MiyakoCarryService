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
    public class CommandMgr : BaseMgr
    {
        public override void Start()
        {
            base.Start();
            RegisterBuiltinCommands();
            EventMgr.Subscribe<McsLeadPlayerExtractedEvent>(HandleMcsLeadPlayerExtracted, this);
        }

        public GamePlayerOwner GamePlayerOwner
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

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            GamePlayerOwner = null;
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            GamePlayerOwner = null;
        }

        public virtual void Update()
        {
            if (!Gameloop.IsVaildGameWorld)
            {
                return;
            }

            if (KeyInput.BetterIsDown(MiyakoCarryServicePlugin.CommandHotKey.Value))
            {
                BuildMainCommandMenu();
            }
        }

        public virtual void ForEachAliveBot(Action<Player> action)
        {
            var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(Singleton<GameWorld>.Instance.MainPlayer.ProfileId);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                action(mcsBotPlayer);
            }
        }

        public virtual void ForEachAliveBot(Action<Player, BodyPartType> action, BodyPartType bodyPartType)
        {
            var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(Singleton<GameWorld>.Instance.MainPlayer.ProfileId);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                action(mcsBotPlayer, bodyPartType);
            }
        }

        #region Menu

        public virtual void BuildMainCommandMenu()
        {
            if (GamePlayerOwner == null)
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

        public virtual void PreBuildCommandMenu(out ActionsReturnClass actionsReturnClass)
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

            var selectedAction = GamePlayerOwner?.AvailableInteractionState?.Value?.SelectedAction;
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

        public virtual void PostBuildCommandMenu(ActionsReturnClass actionsReturnClass)
        {
            if (actionsReturnClass != null)
            {
                actionsReturnClass.Actions.Add(MakeCommand(Locales.CANCELCOMMAND_NAME, Locales.CANCELCOMMAND_TARGETNAME, false, CloseCommandMenuAction));
                actionsReturnClass.InitSelected();
            }

            if (GamePlayerOwner == null)
            {
                return;
            }

            GamePlayerOwner.AvailableInteractionState.Value = actionsReturnClass;
        }

        public virtual void CloseCommandMenuAction()
        {
            GamePlayerOwner.AvailableInteractionState.Value = new ActionsReturnClass();
        }

        // public virtual void BuildTeamCommandMenu(List<Player> mcsBotPlayers)
        // {
        //     PreBuildCommandMenu(out var actionsReturnClass);

        //     actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMREPORTABOUTENEMYCOMMAND_NAME, Locales.TEAMREPORTABOUTENEMYCOMMAND_TARGETNAME, ReportAboutEnemyCommandAction));
        //     actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMONYOUROWNCOMMAND_NAME, Locales.TEAMONYOUROWNCOMMAND_TARGETNAME, OnYourOwnCommandAction));
        //     actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMREGROUPCOMMAND_NAME, Locales.TEAMREGROUPCOMMAND_TARGETNAME, RegroupCommandAction));
        //     actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMGOTOPOINTCOMMAND_NAME, Locales.TEAMGOTOPOINTCOMMAND_TARGETNAME, GoToPointCommandAction));
        //     actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMHOLDPOSITIONCOMMAND_NAME, Locales.TEAMHOLDPOSITIONCOMMAND_TARGETNAME, HoldPositionCommandAction));
        //     actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMDROPTARGETLOOTCOMMAND_NAME, Locales.TEAMDROPTARGETLOOTCOMMAND_TARGETNAME, DropTargetLootCommandAction));
        //     actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamEscortCommandMenu, mcsBotPlayers, Locales.TEAMESCORTCOMMAND_NAME, Locales.TEAMESCORTCOMMAND_TARGETNAME));
        //     actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamAimingTypeCommandMenu, mcsBotPlayers, Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME));
        //     actionsReturnClass.Actions.Add(MakeTeamCommand(Locales.TEAMFORCETELEPORTCOMMAND_NAME, Locales.TEAMFORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction));

        //     PostBuildCommandMenu(actionsReturnClass);
        // }

        // public virtual void BuildMemberCommandMenu(Player mcsBotPlayer)
        // {
        //     PreBuildCommandMenu(out var actionsReturnClass);

        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.REPORTABOUTENEMYCOMMAND_NAME, Locales.REPORTABOUTENEMYCOMMAND_TARGETNAME, ReportAboutEnemyCommandAction, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.ONYOUROWNCOMMAND_NAME, Locales.ONYOUROWNCOMMAND_TARGETNAME, OnYourOwnCommandAction, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.REGROUPCOMMAND_NAME, Locales.REGROUPCOMMAND_TARGETNAME, RegroupCommandAction, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.GOTOPOINTCOMMAND_NAME, Locales.GOTOPOINTCOMMAND_TARGETNAME, GoToPointCommandAction, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.HOLDPOSITIONCOMMAND_NAME, Locales.HOLDPOSITIONCOMMAND_TARGETNAME, HoldPositionCommandAction, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.DROPTARGETLOOTCOMMAND_NAME, Locales.DROPTARGETLOOTCOMMAND_TARGETNAME, DropTargetLootCommandAction, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.OPENINVENTORYCOMMAND_NAME, Locales.OPENINVENTORYCOMMAND_TARGETNAME, MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction, OpenInventoryCommandAction, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME, BuildAimingTypeCommandMenu, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.ESCORTCOMMAND_NAME, Locales.ESCORTCOMMAND_TARGETNAME, BuildEscortCommandMenu, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.PROXYCOMMAND_NAME, Locales.PROXYCOMMAND_TARGETNAME, BuildProxyCommandMenu, mcsBotPlayer));
        //     actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.FORCETELEPORTCOMMAND_NAME, Locales.FORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction, mcsBotPlayer));

        //     PostBuildCommandMenu(actionsReturnClass);
        // }

        private void RegisterBuiltinCommands()
        {
            void Leaf(ECommandScope scope, string nameKey, string targetKey, Action<Player> execute, Func<Player, bool> disabled = null)
                => CommandUtils.RegisterCommand(new DelegateCommand(nameKey, targetKey, scope, isSubMenu: false, disabled, execute));

            void MemberSubMenu(string nameKey, string targetKey, Action<Player> openMenu)
                => CommandUtils.RegisterCommand(new DelegateCommand(nameKey, targetKey, ECommandScope.Member, isSubMenu: true, disabled: null, execute: openMenu));

            void TeamSubMenu(string nameKey, string targetKey, Action<List<Player>> openMenu)
                => CommandUtils.RegisterCommand(new DelegateCommand(nameKey, targetKey, ECommandScope.Team, isSubMenu: true, disabled: null, execute: null, executeTeam: openMenu));

            // ---------------- Member ----------------  
            Leaf(ECommandScope.Member, Locales.REPORTABOUTENEMYCOMMAND_NAME, Locales.REPORTABOUTENEMYCOMMAND_TARGETNAME, ReportAboutEnemyCommandAction);
            Leaf(ECommandScope.Member, Locales.ONYOUROWNCOMMAND_NAME, Locales.ONYOUROWNCOMMAND_TARGETNAME, OnYourOwnCommandAction);
            Leaf(ECommandScope.Member, Locales.REGROUPCOMMAND_NAME, Locales.REGROUPCOMMAND_TARGETNAME, RegroupCommandAction);
            Leaf(ECommandScope.Member, Locales.GOTOPOINTCOMMAND_NAME, Locales.GOTOPOINTCOMMAND_TARGETNAME, GoToPointCommandAction);
            Leaf(ECommandScope.Member, Locales.HOLDPOSITIONCOMMAND_NAME, Locales.HOLDPOSITIONCOMMAND_TARGETNAME, HoldPositionCommandAction);
            Leaf(ECommandScope.Member, Locales.DROPTARGETLOOTCOMMAND_NAME, Locales.DROPTARGETLOOTCOMMAND_TARGETNAME, DropTargetLootCommandAction);
            Leaf(ECommandScope.Member, Locales.OPENINVENTORYCOMMAND_NAME, Locales.OPENINVENTORYCOMMAND_TARGETNAME, OpenInventoryCommandAction, disabled: _ => MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction);
            MemberSubMenu(Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME, BuildAimingTypeCommandMenu);
            MemberSubMenu(Locales.ESCORTCOMMAND_NAME, Locales.ESCORTCOMMAND_TARGETNAME, BuildEscortCommandMenu);
            MemberSubMenu(Locales.PROXYCOMMAND_NAME, Locales.PROXYCOMMAND_TARGETNAME, BuildProxyCommandMenu);
            Leaf(ECommandScope.Member, Locales.FORCETELEPORTCOMMAND_NAME, Locales.FORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction);

            // ---------------- Team ----------------  
            Leaf(ECommandScope.Team, Locales.TEAMREPORTABOUTENEMYCOMMAND_NAME, Locales.TEAMREPORTABOUTENEMYCOMMAND_TARGETNAME, ReportAboutEnemyCommandAction);
            Leaf(ECommandScope.Team, Locales.TEAMONYOUROWNCOMMAND_NAME, Locales.TEAMONYOUROWNCOMMAND_TARGETNAME, OnYourOwnCommandAction);
            Leaf(ECommandScope.Team, Locales.TEAMREGROUPCOMMAND_NAME, Locales.TEAMREGROUPCOMMAND_TARGETNAME, RegroupCommandAction);
            Leaf(ECommandScope.Team, Locales.TEAMGOTOPOINTCOMMAND_NAME, Locales.TEAMGOTOPOINTCOMMAND_TARGETNAME, GoToPointCommandAction);
            Leaf(ECommandScope.Team, Locales.TEAMHOLDPOSITIONCOMMAND_NAME, Locales.TEAMHOLDPOSITIONCOMMAND_TARGETNAME, HoldPositionCommandAction);
            Leaf(ECommandScope.Team, Locales.TEAMDROPTARGETLOOTCOMMAND_NAME, Locales.TEAMDROPTARGETLOOTCOMMAND_TARGETNAME, DropTargetLootCommandAction);
            TeamSubMenu(Locales.TEAMESCORTCOMMAND_NAME, Locales.TEAMESCORTCOMMAND_TARGETNAME, BuildTeamEscortCommandMenu);
            TeamSubMenu(Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME, BuildTeamAimingTypeCommandMenu);
            Leaf(ECommandScope.Team, Locales.TEAMFORCETELEPORTCOMMAND_NAME, Locales.TEAMFORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction);
        }

        private void BuildMemberCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var commands = CommandUtils.GetCommands();
            foreach (var cmd in commands.Where(c => c.Scope == ECommandScope.Member))
            {
                actionsReturnClass.Actions.Add(new ActionsTypesClass
                {
                    Name = cmd.NameKey,
                    TargetName = cmd.TargetNameKey,
                    Disabled = cmd.IsDisabled(mcsBotPlayer),
                    Action = () => cmd.Execute(mcsBotPlayer)
                });
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        private void BuildTeamCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);
            var commands = CommandUtils.GetCommands();
            foreach (var cmd in commands.Where(c => c.Scope == ECommandScope.Team))
            {
                if (cmd.IsSubMenu)
                {
                    actionsReturnClass.Actions.Add(new ActionsTypesClass
                    {
                        Name = cmd.NameKey,
                        TargetName = cmd.TargetNameKey,
                        Disabled = mcsBotPlayers.All(p => !p.HealthController.IsAlive),
                        Action = () => cmd.ExecuteTeam(mcsBotPlayers)
                    });
                }
                else
                {
                    actionsReturnClass.Actions.Add(new ActionsTypesClass
                    {
                        Name = cmd.NameKey,
                        TargetName = cmd.TargetNameKey,
                        Disabled = false,
                        Action = () => ForEachAliveBot(cmd.Execute)
                    });
                }
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildTeamEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamQuestEscortCommandMenu, mcsBotPlayers, Locales.TEAMQUESTESCORTCOMMAND_NAME, Locales.TEAMQUESTESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamTransitEscortCommandMenu, mcsBotPlayers, Locales.TEAMTRANSITESCORTCOMMAND_NAME, Locales.TEAMTRANSITESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamExfilEscortCommandMenu, mcsBotPlayers, Locales.TEAMEXFILESCORTCOMMAND_NAME, Locales.TEAMEXFILESCORTCOMMAND_TARGETNAME));
            actionsReturnClass.Actions.Add(TeamCommandSubMenu(BuildTeamSwitchEscortCommandMenu, mcsBotPlayers, Locales.TEAMSWITCHESCORTCOMMAND_NAME, Locales.TEAMSWITCHESCORTCOMMAND_TARGETNAME));

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.QUESTESCORTCOMMAND_NAME, Locales.QUESTESCORTCOMMAND_TARGETNAME, BuildQuestEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.TRANSITESCORTCOMMAND_NAME, Locales.TRANSITESCORTCOMMAND_TARGETNAME, BuildTransitEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.EXFILESCORTCOMMAND_NAME, Locales.EXFILESCORTCOMMAND_TARGETNAME, BuildExfilEscortCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.SWITCHESCORTCOMMAND_NAME, Locales.SWITCHESCORTCOMMAND_TARGETNAME, BuildSwitchEscortCommandMenu, mcsBotPlayer));

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildTeamAimingTypeCommandMenu(List<Player> mcsBotPlayers)
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

        public virtual void BuildAimingTypeCommandMenu(Player mcsBotPlayer)
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

        public virtual void BuildTeamTransitEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var transitDatas = Gameloop.GetDatas<TransitData, TransitDataMgr>();
            foreach (var transitData in transitDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayers, transitData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildTeamExfilEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var exfilDatas = Gameloop.GetDatas<ExfilData, ExfilDataMgr>();
            foreach (var exfilData in exfilDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayers, exfilData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildTeamSwitchEscortCommandMenu(List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var switchDatas = Gameloop.GetDatas<SwitchData, SwitchDataMgr>();
            foreach (var switchData in switchDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayers, switchData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildTeamQuestEscortCommandMenu(List<Player> mcsBotPlayers)
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

        public virtual void BuildQuestEscortCommandMenu(Player mcsBotPlayer)
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

        public virtual void BuildTransitEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var transitDatas = Gameloop.GetDatas<TransitData, TransitDataMgr>();
            foreach (var transitData in transitDatas)
            {
                actionsReturnClass.Actions.Add(EscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayer, transitData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildExfilEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var exfilDatas = Gameloop.GetDatas<ExfilData, ExfilDataMgr>();
            foreach (var exfilData in exfilDatas)
            {
                actionsReturnClass.Actions.Add(EscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayer, exfilData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildSwitchEscortCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var switchDatas = Gameloop.GetDatas<SwitchData, SwitchDataMgr>();
            foreach (var switchData in switchDatas)
            {
                actionsReturnClass.Actions.Add(EscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayer, switchData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildTeamSubQuestEscortCommandMenu(List<QuestData> questDatas, List<Player> mcsBotPlayers)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            foreach (var questData in questDatas)
            {
                actionsReturnClass.Actions.Add(TeamEscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayers, questData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildSubQuestEscortCommandMenu(List<QuestData> questDatas, Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            foreach (var questData in questDatas)
            {
                actionsReturnClass.Actions.Add(EscortToWorldPosCommand(EscortToWorldPosCommandAction, mcsBotPlayer, questData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildProxyCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.QUESTPROXYCOMMAND_NAME, Locales.QUESTPROXYCOMMAND_TARGETNAME, BuildQuestProxyCommandMenu, mcsBotPlayer));
            actionsReturnClass.Actions.Add(MakeMemberCommand(Locales.SWITCHPROXYCOMMAND_NAME, Locales.SWITCHPROXYCOMMAND_TARGETNAME, BuildSwitchProxyCommandMenu, mcsBotPlayer));

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildQuestProxyCommandMenu(Player mcsBotPlayer)
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

        public virtual void BuildSwitchProxyCommandMenu(Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            var switchDatas = Gameloop.GetDatas<SwitchData, SwitchDataMgr>();
            foreach (var switchData in switchDatas)
            {
                actionsReturnClass.Actions.Add(ExcuteCommonProxyActionCommand(InteractionProxyActionCommandAction, mcsBotPlayer, switchData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public virtual void BuildSubQuestProxyCommandMenu(List<QuestData> questDatas, Player mcsBotPlayer)
        {
            PreBuildCommandMenu(out var actionsReturnClass);

            foreach (var questData in questDatas)
            {
                actionsReturnClass.Actions.Add(ExcuteQuestProxyActionCommand(QuestProxyActionCommandAction, mcsBotPlayer, questData));
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        #endregion

        public virtual ActionsTypesClass MakeCommand(string name, string targetName, bool disabled, Action action)
        {
            return new ActionsTypesClass { Name = name, TargetName = targetName, Disabled = disabled, Action = action };
        }

        public virtual ActionsTypesClass MakeCommand(string name, string targetName, bool disabled, Action<List<Player>> action, List<Player> mcsBotPlayers)
        {
            return new ActionsTypesClass { Name = name, TargetName = targetName, Disabled = disabled, Action = () => action(mcsBotPlayers) };
        }

        public virtual ActionsTypesClass MakeMemberCommand(string name, string targetName, Action<Player> action, Player mcsBotPlayer)
        {
            return MakeCommand(name, targetName, false, () => action(mcsBotPlayer));
        }

        public virtual ActionsTypesClass MakeMemberCommand(string name, string targetName, bool disabled, Action<Player, BodyPartType> action, Player mcsBotPlayer, BodyPartType bodyPartType)
        {
            return MakeCommand(name, targetName, disabled, () => action(mcsBotPlayer, bodyPartType));
        }

        public virtual ActionsTypesClass MakeMemberCommand(string name, string targetName, bool disabled, Action<Player> action, Player mcsBotPlayer)
        {
            return MakeCommand(name, targetName, disabled, () => action(mcsBotPlayer));
        }

        public virtual ActionsTypesClass MakeTeamCommand(string name, string targetName, Action<Player> action)
        {
            return MakeCommand(name, targetName, false, () => ForEachAliveBot(action));
        }

        public virtual ActionsTypesClass MakeTeamCommand(string name, string targetName, Action<Player, BodyPartType> action, BodyPartType bodyPartType)
        {
            return MakeCommand(name, targetName, false, () => ForEachAliveBot(action, bodyPartType));
        }

        public virtual ActionsTypesClass TeamCommandSubMenu(Action<List<Player>> action, List<Player> mcsBotPlayers)
        {
            return TeamCommandSubMenu(action, mcsBotPlayers, Locales.TEAMCOMMAND_NAME, Locales.TEAMCOMMAND_TARGETNAME);
        }

        public virtual ActionsTypesClass TeamCommandSubMenu(Action<List<Player>> action, List<Player> mcsBotPlayers, string name, string targetName)
        {
            return MakeCommand(name, targetName, mcsBotPlayers.All(p => !p.HealthController.IsAlive), action, mcsBotPlayers);
        }

        public virtual ActionsTypesClass MakeMemberCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return MakeMemberCommand(mcsBotPlayer.Profile.Info.Nickname, Locales.MEMBERCOMMAND_TARGETNAME, !mcsBotPlayer.HealthController.IsAlive, action, mcsBotPlayer);
        }

        public virtual ActionsTypesClass TeamEscortToWorldPosCommand(Action<Player, WorldData> action, List<Player> mcsBotPlayers, WorldData worldData)
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

        public virtual ActionsTypesClass EscortToWorldPosCommand(Action<Player, WorldData> action, Player mcsBotPlayer, WorldData worldData)
        {
            return new ActionsTypesClass
            {
                Name = worldData.GetActionName(),
                TargetName = worldData.GetActionTargetName(Singleton<GameWorld>.Instance.MainPlayer.Position),
                Disabled = worldData.IsDisabled(),
                Action = () => action(mcsBotPlayer, worldData)
            };
        }

        public virtual ActionsTypesClass ExcuteQuestProxyActionCommand(Action<Player, QuestData> action, Player mcsBotPlayer, QuestData questData)
        {
            return new ActionsTypesClass
            {
                Name = questData.GetActionName(),
                TargetName = questData.GetActionTargetName(Singleton<GameWorld>.Instance.MainPlayer.Position),
                Disabled = questData.IsProxyActionDisabled(),
                Action = () => action(mcsBotPlayer, questData)
            };
        }

        public virtual ActionsTypesClass ExcuteCommonProxyActionCommand(Action<Player, IProxyActor> action, Player mcsBotPlayer, IProxyActor interactiveObjectData)
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

        public void DispatchCommand(
            Player mcsBotPlayer,
            string packetType,
            Action<Player> localAction,
            Vector3? position = null,
            BodyPartType aimingBodyPartType = BodyPartType.head,
            string targetId = null)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                EventMgr.Notify(new CommandMgrHandleFikaEvent
                {
                    McsBotPlayer = mcsBotPlayer,
                    CommandPacketType = packetType,
                    Position = position,
                    AimingBodyPartType = aimingBodyPartType,
                    TargetId = targetId
                });
            }
            else
            {
                localAction(mcsBotPlayer);
            }
            CloseCommandMenuAction();
        }

        // public virtual void EscortToWorldPosCommandAction(Player mcsBotPlayer, WorldData worldData)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.Escort.ToString(),
        //             Position = worldData.GetPos()
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         if (botOwner.Memory.HaveEnemy)
        //         {
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.Negative,
        //             });
        //         }
        //         botOwner.Mover.LastTimePosChanged = Time.time;
        //         botOwner.StopMove();
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldEscort);
        //             mcsBotPlayerData.TargetPos = worldData.GetPos();
        //             mcsBotPlayerData.IsLooting = false;
        //         }
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void EscortToWorldPosCommandAction(Player mcsBotPlayer, WorldData worldData)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.Regroup.ToString(), p =>
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
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldEscort);
                    mcsBotPlayerData.TargetPos = worldData.GetPos();
                    mcsBotPlayerData.IsLooting = false;
                }
            }, worldData.GetPos());
        }

        // public virtual void ReportAboutEnemyCommandAction(Player mcsBotPlayer)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.ReportAboutEnemy.ToString()
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.IsLooting = false;
        //             mcsBotPlayerData.TargetPos = null;
        //             mcsBotPlayerData.ProxyTargetId = null;
        //         }
        //         if (botOwner.Memory.HaveEnemy)
        //         {
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.OnFirstContact,
        //                 Position = botOwner.Memory.GoalEnemy.EnemyLastPosition
        //             });
        //         }
        //         else
        //         {
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.Clear,
        //             });
        //         }
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void ReportAboutEnemyCommandAction(Player mcsBotPlayer)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.ReportAboutEnemy.ToString(), p =>
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
            });
        }

        // public virtual void OnYourOwnCommandAction(Player mcsBotPlayer)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.OnYourOwn.ToString(),
        //             Position = null
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.SetDecision();
        //             mcsBotPlayerData.IsLooting = false;
        //             mcsBotPlayerData.TargetPos = null;
        //             mcsBotPlayerData.ProxyTargetId = null;
        //         }
        //         botOwner.TalkMsg(new McsMsg
        //         {
        //             PhraseTrigger = EPhraseTrigger.Roger,
        //         });
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void OnYourOwnCommandAction(Player mcsBotPlayer)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.OnYourOwn.ToString(), p =>
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
            });
        }

        // public virtual void RegroupCommandAction(Player mcsBotPlayer)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.Regroup.ToString(),
        //             Position = null
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.SetDecision(null, Decisions.ShouldRegroup);
        //             mcsBotPlayerData.IsLooting = false;
        //             mcsBotPlayerData.TargetPos = null;
        //             mcsBotPlayerData.ProxyTargetId = null;
        //         }
        //         botOwner.TalkMsg(new McsMsg
        //         {
        //             PhraseTrigger = EPhraseTrigger.Regroup,
        //         });
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void RegroupCommandAction(Player mcsBotPlayer)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.Regroup.ToString(), p =>
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.SetDecision(null, Decisions.ShouldRegroup);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Regroup,
                });
            });
        }

        // public virtual void GoToPointCommandAction(Player mcsBotPlayer)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask))
        //         {
        //             EventMgr.Notify(new CommandMgrHandleFikaEvent
        //             {
        //                 McsBotPlayer = mcsBotPlayer,
        //                 CommandPacketType = ECommandPacketType.GoToPoint.ToString(),
        //                 Position = raycastHit.point
        //             });
        //         }
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask))
        //         {
        //             var pos = Tools.GetPosNearTarget(raycastHit.point, botOwner);
        //             if (pos.HasValue)
        //             {
        //                 botOwner.TalkMsg(new McsMsg
        //                 {
        //                     PhraseTrigger = EPhraseTrigger.Going,
        //                 });

        //                 var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //                 if (mcsBotPlayerData != null)
        //                 {
        //                     mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldGoToPoint);
        //                     mcsBotPlayerData.IsLooting = false;
        //                     mcsBotPlayerData.TargetPos = pos.Value;
        //                     mcsBotPlayerData.ProxyTargetId = null;
        //                 }
        //                 botOwner.Mover.LastTimePosChanged = Time.time;
        //                 botOwner.StopMove();
        //             }
        //         }
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void GoToPointCommandAction(Player mcsBotPlayer)
        {
            if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask))
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                var pos = Tools.GetPosNearTarget(raycastHit.point, botOwner);
                if (pos.HasValue)
                {
                    DispatchCommand(mcsBotPlayer, ECommandPacketType.GoToPoint.ToString(), p =>
                    {
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Going,
                        });

                        var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                        if (mcsBotPlayerData != null)
                        {
                            mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldGoToPoint);
                            mcsBotPlayerData.IsLooting = false;
                            mcsBotPlayerData.TargetPos = pos.Value;
                            mcsBotPlayerData.ProxyTargetId = null;
                        }
                        botOwner.Mover.LastTimePosChanged = Time.time;
                        botOwner.StopMove();
                    }, pos);
                    return;
                }
            }
            CloseCommandMenuAction();
        }

        // public virtual void ChangeAimingBodyPartCommandAction(Player mcsBotPlayer, BodyPartType aimingBodyPartType)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.AimingBodyPart.ToString(),
        //             Position = null,
        //             AimingBodyPartType = aimingBodyPartType
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.AimingBodyPartType = aimingBodyPartType;
        //         }
        //         botOwner.TalkMsg(new McsMsg
        //         {
        //             PhraseTrigger = EPhraseTrigger.Roger,
        //         });
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void ChangeAimingBodyPartCommandAction(Player mcsBotPlayer, BodyPartType aimingBodyPartType)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.AimingBodyPart.ToString(), p =>
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
            }, null, aimingBodyPartType);
        }

        // public virtual void HoldPositionCommandAction(Player mcsBotPlayer)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.HoldPosition.ToString(),
        //             Position = null
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         botOwner.StopMove();
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldHoldPosition);
        //             mcsBotPlayerData.IsLooting = false;
        //             mcsBotPlayerData.TargetPos = null;
        //             mcsBotPlayerData.ProxyTargetId = null;
        //         }
        //         botOwner.TalkMsg(new McsMsg
        //         {
        //             PhraseTrigger = EPhraseTrigger.HoldPosition,
        //         });
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void HoldPositionCommandAction(Player mcsBotPlayer)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.HoldPosition.ToString(), p =>
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                botOwner.StopMove();
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldHoldPosition);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.HoldPosition,
                });
            });
        }

        // public virtual void DropTargetLootCommandAction(Player mcsBotPlayer)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.ThrowTargetLoot.ToString()
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         if (botOwner.ExternalItemsController.HaveItemsToDrop())
        //         {
        //             botOwner.StopMove();
        //             var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //             if (mcsBotPlayerData != null)
        //             {
        //                 mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldThrowTargetLoot);
        //                 mcsBotPlayerData.IsLooting = false;
        //                 mcsBotPlayerData.TargetPos = null;
        //                 mcsBotPlayerData.ProxyTargetId = null;
        //             }
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.Roger,
        //             });
        //         }
        //         else
        //         {
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.Negative,
        //             });
        //         }
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void DropTargetLootCommandAction(Player mcsBotPlayer)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.ThrowTargetLoot.ToString(), p =>
            {
                var botOwner = mcsBotPlayer.AIData.BotOwner;
                if (botOwner.ExternalItemsController.HaveItemsToDrop())
                {
                    botOwner.StopMove();
                    var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                    if (mcsBotPlayerData != null)
                    {
                        mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldThrowTargetLoot);
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
            });
        }

        // public virtual void ForceTeleportCommandAction(Player mcsBotPlayer)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.Teleport.ToString(),
        //             Position = null
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         botOwner.StopMove();
        //         botOwner.Mover.AllowTeleport();
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.SetDecision();
        //             mcsBotPlayerData.IsLooting = false;
        //             mcsBotPlayerData.TargetPos = null;
        //             mcsBotPlayerData.ProxyTargetId = null;
        //             mcsBotPlayer.Teleport(mcsBotPlayerData.LeadPlayer.Position, true);
        //         }
        //         var playerPosition = mcsBotPlayer.Position;
        //         botOwner.Mover.LastGoodCastPoint = botOwner.Mover.PrevSuccessLinkedFrom_1 = botOwner.Mover.PrevLinkPos = botOwner.Mover.PositionOnWayInner = playerPosition;
        //         botOwner.Mover.LastGoodCastPointTime = Time.time;
        //         botOwner.Mover.PrevPosLinkedTime_1 = 0f;
        //         botOwner.Mover.SetPlayerToNavMesh(playerPosition);
        //         botOwner.Mover.RecalcWay();
        //         botOwner.Mover.Pause = true;
        //         botOwner.TalkMsg(new McsMsg
        //         {
        //             PhraseTrigger = EPhraseTrigger.Roger,
        //         });

        //         if (!MiyakoCarryServicePlugin.SAINInstalled)
        //         {
        //             botOwner.Memory.GoalTarget.Clear();
        //             botOwner.Memory.GoalEnemy = null;
        //         }
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void ForceTeleportCommandAction(Player mcsBotPlayer)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.Teleport.ToString(), p =>
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
            });
        }

        public virtual void OpenInventoryCommandAction(Player mcsBotPlayer)
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
                        owner = GamePlayerOwner,
                        rootItem = rootItem,
                        lootItemOwner = traderControllerClass
                    };
                    inventoryActionClass.method_3();
                }
            }
            CloseCommandMenuAction();
        }

        public virtual void HandleMcsLeadPlayerExtracted(McsLeadPlayerExtractedEvent @event)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
            {
                var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(@event.McsLeadPlayerId);
                foreach (var mcsBotPlayer in mcsBotPlayers)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.GoToExfil.ToString(),
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
                    mcsBotPlayerData.SetDecision(null, Decisions.ShouldExfil);
                }
            }
        }

        // public virtual void InteractionProxyActionCommandAction(Player mcsBotPlayer, IProxyActor proxyAction)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.InteractionProxyAction.ToString(),
        //             TargetId = proxyAction.Id()
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         if (botOwner.Memory.HaveEnemy)
        //         {
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.Negative,
        //             });
        //         }
        //         botOwner.Mover.LastTimePosChanged = Time.time;
        //         botOwner.StopMove();
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldInteractionProxyAction);
        //             var interactableObjectData = Singleton<GameWorld>.Instance.FindInteractableObjectData(proxyAction.Id());
        //             if (interactableObjectData != null)
        //             {
        //                 mcsBotPlayerData.ProxyTargetId = proxyAction.Id();
        //                 mcsBotPlayerData.TargetPos = interactableObjectData.GetPos();
        //                 botOwner.TalkMsg(new McsMsg
        //                 {
        //                     PhraseTrigger = EPhraseTrigger.Roger,
        //                 });
        //             }
        //             mcsBotPlayerData.IsLooting = false;
        //         }
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void InteractionProxyActionCommandAction(Player mcsBotPlayer, IProxyActor proxyAction)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.InteractionProxyAction.ToString(), p =>
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
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldInteractionProxyAction);
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
            }, targetId: proxyAction.Id());
        }

        // public virtual void QuestProxyActionCommandAction(Player mcsBotPlayer, QuestData questData)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.QuestProxyAction.ToString(),
        //             Position = questData.GetPos(),
        //             TargetId = questData.Id()
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         if (botOwner.Memory.HaveEnemy)
        //         {
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.Negative,
        //             });
        //         }
        //         botOwner.Mover.LastTimePosChanged = Time.time;
        //         botOwner.StopMove();
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldQuestProxyAction);
        //             mcsBotPlayerData.ProxyTargetId = questData.Id();
        //             mcsBotPlayerData.TargetPos = questData.GetPos();
        //             mcsBotPlayerData.IsLooting = false;
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.Roger,
        //             });
        //         }
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void QuestProxyActionCommandAction(Player mcsBotPlayer, QuestData questData)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.QuestProxyAction.ToString(), p =>
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
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldQuestProxyAction);
                    mcsBotPlayerData.ProxyTargetId = questData.Id();
                    mcsBotPlayerData.TargetPos = questData.GetPos();
                    mcsBotPlayerData.IsLooting = false;
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Roger,
                    });
                }
            }, position: questData.GetPos(), targetId: questData.Id());
        }

        // public virtual void LootProxyActionCommandAction(Player mcsBotPlayer, LootData lootData)
        // {
        //     if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
        //     {
        //         EventMgr.Notify(new CommandMgrHandleFikaEvent
        //         {
        //             McsBotPlayer = mcsBotPlayer,
        //             CommandPacketType = ECommandPacketType.LootProxyAction.ToString(),
        //             Position = lootData.RootTransform.position,
        //             TargetId = lootData.Item.Id
        //         });
        //     }
        //     else
        //     {
        //         var botOwner = mcsBotPlayer.AIData.BotOwner;
        //         var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
        //         if (botOwner.Memory.HaveEnemy)
        //         {
        //             botOwner.TalkMsg(new McsMsg
        //             {
        //                 PhraseTrigger = EPhraseTrigger.Negative,
        //             });
        //             if (mcsBotPlayerData != null)
        //             {
        //                 mcsBotPlayerData.ProxyTargetId = null;
        //                 mcsBotPlayerData.TargetPos = null;
        //             }
        //         }
        //         botOwner.Mover.LastTimePosChanged = Time.time;
        //         botOwner.StopMove();

        //         if (mcsBotPlayerData != null)
        //         {
        //             mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldLootProxyAction);
        //             mcsBotPlayerData.IsLooting = false;
        //             mcsBotPlayerData.ProxyTargetId = lootData.Item.Id;
        //             mcsBotPlayerData.TargetPos = lootData.RootTransform.position;
        //             LootDataMgr.UnlockLootingTarget(lootData);
        //             LootDataMgr.UnlockLootingTargetRootTransform(lootData.RootTransform);
        //             if (!LootDataMgr.IsLockedLootingTarget(lootData) && !LootDataMgr.IsLockedLootingTargetRootTransform(lootData.RootTransform))
        //             {
        //                 LootDataMgr.LockLootItemToTarget(lootData);
        //                 LootDataMgr.LockLootingTargetRootTransform(lootData.RootTransform);
        //                 mcsBotPlayerData.LootingTarget = lootData;
        //                 botOwner.TalkMsg(new McsMsg
        //                 {
        //                     PhraseTrigger = EPhraseTrigger.Roger,
        //                 });
        //             }
        //             else
        //             {
        //                 botOwner.TalkMsg(new McsMsg
        //                 {
        //                     PhraseTrigger = EPhraseTrigger.Negative,
        //                 });
        //                 mcsBotPlayerData.RemoveDecision(Decisions.ShouldLootProxyAction);
        //                 mcsBotPlayerData.ProxyTargetId = null;
        //                 mcsBotPlayerData.TargetPos = null;
        //             }
        //         }
        //     }
        //     CloseCommandMenuAction();
        // }

        public virtual void LootProxyActionCommandAction(Player mcsBotPlayer, LootData lootData)
        {
            DispatchCommand(mcsBotPlayer, ECommandPacketType.LootProxyAction.ToString(), p =>
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
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldLootProxyAction);
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
                        mcsBotPlayerData.RemoveDecision(Decisions.ShouldLootProxyAction);
                        mcsBotPlayerData.ProxyTargetId = null;
                        mcsBotPlayerData.TargetPos = null;
                    }
                }
            }, position: lootData.RootTransform.position, targetId: lootData.Item.Id);
        }

        #endregion
    }
}