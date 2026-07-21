using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using UnityEngine;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPT.Common.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public class CommandMgr : BaseMgr
    {
        public sealed override void Start()
        {
            base.Start();
            EventMgr.Subscribe<McsLeadPlayerExtractedEvent>(HandleMcsLeadPlayerExtracted, this);
            CommandUtils.RegisterCommandHandler(ECommandType.EscortWorld.ToString(), EscortToWorldPosCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.GoToPoint.ToString(), GoToPointCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.Teleport.ToString(), ForceTeleportCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.QuestProxyAction.ToString(), QuestProxyActionCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.LootProxyAction.ToString(), LootProxyActionCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.InteractionProxyAction.ToString(), InteractionProxyActionCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.Regroup.ToString(), RegroupCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.HoldPosition.ToString(), HoldPositionCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.OnYourOwn.ToString(), OnYourOwnCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.ReportAboutEnemy.ToString(), ReportAboutEnemyCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.ReportAboutSelf.ToString(), ReportAboutSelfCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.AimingBodyPart.ToString(), ChangeAimingBodyPartCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.DropTargetLoot.ToString(), DropTargetLootCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.ClearArea.ToString(), ClearAreaCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.OpenInventory.ToString(), OpenInventoryCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.GoToExfil.ToString(), GoToExfilCommandAction);
            CommandUtils.RegisterCommandHandler(ECommandType.ChangeFormation.ToString(), ChangeFormationCommandAction);
        }

        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        private LootDataMgr LootDataMgr => MgrAccessor.Get<LootDataMgr>();
        private FormationDataMgr FormationDataMgr => MgrAccessor.Get<FormationDataMgr>();

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            CommandUtils.ClearGamePlayerOwner();
            CommandUtils.ClearMenuStack();
            CommandUtils.NavMeshCache();
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            CommandUtils.ClearGamePlayerOwner();
            CommandUtils.ClearMenuStack();
            CommandUtils.ClearNavMeshCache();
        }

        void Update()
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

        public Player[] GetMembers()
        {
            return McsMgr.GetAllMcsSquadMembersByMcsLeadId(Singleton<GameWorld>.Instance.MainPlayer.ProfileId).ToArray();
        }

        #region Menu  

        public virtual void BuildMainCommandMenu()
        {
            if (CommandUtils.GamePlayerOwner == null)
            {
                return;
            }

            CommandUtils.ClearMenuStack();
            CommandUtils.OpenMenu(BuildMainMenu);
        }

        public virtual void BuildMainMenu(McsCommandMenu menu)
        {
            var mcsBotPlayers = GetMembers();
            if (mcsBotPlayers.Length == 0)
            {
                return;
            }

            menu.RegisterSubMenu(Locales.TEAMCOMMAND_NAME, Locales.TEAMCOMMAND_TARGETNAME, m => BuildTeamMenu(m, mcsBotPlayers), disabled: () => mcsBotPlayers.All(p => !p.HealthController.IsAlive));

            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                menu.RegisterSubMenu(mcsBotPlayer.Profile.Info.Nickname, Locales.MEMBERCOMMAND_TARGETNAME, m => BuildMemberMenu(m, [mcsBotPlayer]), disabled: () => !mcsBotPlayer.HealthController.IsAlive);
            }

            menu.RegisterSubMenu(Locales.CHANGEFORMATIONCOMMAND_NAME, Locales.CHANGEFORMATIONCOMMAND_TARGETNAME, m => BuildFormationMenu(m, [mcsBotPlayers.FirstOrDefault()]));

            // 不打算对根菜单进行扩展
            // CommandUtils.Apply(EMenuId.Main.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildTeamMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterCommand(Locales.TEAMREPORTABOUTENEMYCOMMAND_NAME, Locales.TEAMREPORTABOUTENEMYCOMMAND_TARGETNAME, ECommandType.ReportAboutEnemy.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMREPORTABOUTSELFCOMMAND_NAME, Locales.TEAMREPORTABOUTSELFCOMMAND_TARGETNAME, ECommandType.ReportAboutSelf.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMONYOUROWNCOMMAND_NAME, Locales.TEAMONYOUROWNCOMMAND_TARGETNAME, ECommandType.OnYourOwn.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMREGROUPCOMMAND_NAME, Locales.TEAMREGROUPCOMMAND_TARGETNAME, ECommandType.Regroup.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMGOTOPOINTCOMMAND_NAME, Locales.TEAMGOTOPOINTCOMMAND_TARGETNAME, ECommandType.GoToPoint.ToString(), mcsBotPlayers, resolver: () => Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay,
            out var hit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask)
            ? new McsCommandContext { Position = hit.point } : null);
            menu.RegisterCommand(Locales.TEAMHOLDPOSITIONCOMMAND_NAME, Locales.TEAMHOLDPOSITIONCOMMAND_TARGETNAME, ECommandType.HoldPosition.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMDROPTARGETLOOTCOMMAND_NAME, Locales.TEAMDROPTARGETLOOTCOMMAND_TARGETNAME, ECommandType.DropTargetLoot.ToString(), mcsBotPlayers);
            menu.RegisterSubMenu(Locales.TEAMESCORTCOMMAND_NAME, Locales.TEAMESCORTCOMMAND_TARGETNAME, m => BuildEscortMenu(m, mcsBotPlayers, true));
            menu.RegisterSubMenu(Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME, m => BuildAimingMenu(m, mcsBotPlayers));
            menu.RegisterCommand(Locales.TEAMCLEARAREACOMMAND_NAME, Locales.TEAMCLEARAREACOMMAND_TARGETNAME, ECommandType.ClearArea.ToString(), mcsBotPlayers, resolver: () => Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay,
            out var hit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask)
            ? new McsCommandContext { Position = hit.point } : null);
            menu.RegisterCommand(Locales.TEAMFORCETELEPORTCOMMAND_NAME, Locales.TEAMFORCETELEPORTCOMMAND_TARGETNAME, ECommandType.Teleport.ToString(), mcsBotPlayers);

            CommandUtils.Apply(EMenuId.Team.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildMemberMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterCommand(Locales.REPORTABOUTENEMYCOMMAND_NAME, Locales.REPORTABOUTENEMYCOMMAND_TARGETNAME, ECommandType.ReportAboutEnemy.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.REPORTABOUTSELFCOMMAND_NAME, Locales.REPORTABOUTSELFCOMMAND_TARGETNAME, ECommandType.ReportAboutSelf.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.ONYOUROWNCOMMAND_NAME, Locales.ONYOUROWNCOMMAND_TARGETNAME, ECommandType.OnYourOwn.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.REGROUPCOMMAND_NAME, Locales.REGROUPCOMMAND_TARGETNAME, ECommandType.Regroup.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.GOTOPOINTCOMMAND_NAME, Locales.GOTOPOINTCOMMAND_TARGETNAME, ECommandType.GoToPoint.ToString(), mcsBotPlayers, resolver: () => Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay,
            out var hit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask)
            ? new McsCommandContext { Position = hit.point } : null);
            menu.RegisterCommand(Locales.HOLDPOSITIONCOMMAND_NAME, Locales.HOLDPOSITIONCOMMAND_TARGETNAME, ECommandType.HoldPosition.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.DROPTARGETLOOTCOMMAND_NAME, Locales.DROPTARGETLOOTCOMMAND_TARGETNAME, ECommandType.DropTargetLoot.ToString(), mcsBotPlayers);
            menu.RegisterCommand(Locales.OPENINVENTORYCOMMAND_NAME, Locales.OPENINVENTORYCOMMAND_TARGETNAME, ECommandType.OpenInventory.ToString(), mcsBotPlayers, isLocal: true, disabled: () => MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction);
            menu.RegisterSubMenu(Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME, m => BuildAimingMenu(m, mcsBotPlayers));
            menu.RegisterSubMenu(Locales.ESCORTCOMMAND_NAME, Locales.ESCORTCOMMAND_TARGETNAME, m => BuildEscortMenu(m, mcsBotPlayers, false));
            menu.RegisterSubMenu(Locales.PROXYCOMMAND_NAME, Locales.PROXYCOMMAND_TARGETNAME, m => BuildProxyMenu(m, mcsBotPlayers));
            menu.RegisterCommand(Locales.CLEARAREACOMMAND_NAME, Locales.CLEARAREACOMMAND_TARGETNAME, ECommandType.ClearArea.ToString(), mcsBotPlayers, resolver: () => Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay,
            out var hit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask)
            ? new McsCommandContext { Position = hit.point } : null);
            menu.RegisterCommand(Locales.FORCETELEPORTCOMMAND_NAME, Locales.FORCETELEPORTCOMMAND_TARGETNAME, ECommandType.Teleport.ToString(), mcsBotPlayers);

            CommandUtils.Apply(EMenuId.Member.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildFormationMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            var formationDatas = FormationDataMgr.GetDatas<FormationData>();
            foreach (var formationData in formationDatas)
            {
                menu.RegisterCommand(
                    formationData.Name,
                    formationData.Name,
                    ECommandType.ChangeFormation.ToString(),
                    mcsBotPlayers,
                    isLocal: true,
                    resolver: () => new McsCommandContext() { TargetId = formationData.Id }
                );
            }
        }

        public virtual void BuildEscortMenu(McsCommandMenu menu, Player[] mcsBotPlayers, bool isTeam)
        {
            menu.RegisterSubMenu(
                isTeam ? Locales.TEAMQUESTESCORTCOMMAND_NAME : Locales.QUESTESCORTCOMMAND_NAME,
                isTeam ? Locales.TEAMQUESTESCORTCOMMAND_TARGETNAME : Locales.QUESTESCORTCOMMAND_TARGETNAME,
                m => BuildQuestEscortMenu(m, mcsBotPlayers));

            menu.RegisterSubMenu(
                isTeam ? Locales.TEAMTRANSITESCORTCOMMAND_NAME : Locales.TRANSITESCORTCOMMAND_NAME,
                isTeam ? Locales.TEAMTRANSITESCORTCOMMAND_TARGETNAME : Locales.TRANSITESCORTCOMMAND_TARGETNAME,
                m => BuildWorldEscortMenu(m, mcsBotPlayers, Gameloop.GetDatas<TransitData, TransitDataMgr>()));

            menu.RegisterSubMenu(
                isTeam ? Locales.TEAMEXFILESCORTCOMMAND_NAME : Locales.EXFILESCORTCOMMAND_NAME,
                isTeam ? Locales.TEAMEXFILESCORTCOMMAND_TARGETNAME : Locales.EXFILESCORTCOMMAND_TARGETNAME,
                m => BuildWorldEscortMenu(m, mcsBotPlayers, Gameloop.GetDatas<ExfilData, ExfilDataMgr>()));

            menu.RegisterSubMenu(
                isTeam ? Locales.TEAMSWITCHESCORTCOMMAND_NAME : Locales.SWITCHESCORTCOMMAND_NAME,
                isTeam ? Locales.TEAMSWITCHESCORTCOMMAND_TARGETNAME : Locales.SWITCHESCORTCOMMAND_TARGETNAME,
                m => BuildWorldEscortMenu(m, mcsBotPlayers, Gameloop.GetDatas<SwitchData, SwitchDataMgr>()));

            CommandUtils.Apply(EMenuId.Escort.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildWorldEscortMenu(McsCommandMenu menu, Player[] mcsBotPlayers, IEnumerable<WorldData> worldDatas)
        {
            var mainPos = Singleton<GameWorld>.Instance.MainPlayer.Position;
            foreach (var worldData in worldDatas)
            {
                menu.RegisterCommand(
                    worldData.GetActionName(),
                    worldData.GetActionTargetName(mainPos),
                    ECommandType.EscortWorld.ToString(), mcsBotPlayers,
                    disabled: worldData.IsDisabled,
                    resolver: () => new McsCommandContext { Position = worldData.GetPos() });
            }

            CommandUtils.Apply(EMenuId.WorldEscort.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildQuestEscortMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            var questDataMgr = MgrAccessor.Get<QuestDataMgr>();
            if (questDataMgr == null)
            {
                return;
            }

            foreach ((var questDataClass, var questDatas) in questDataMgr.GetQuestDataByGroup())
            {
                menu.RegisterSubMenu(questDataClass.Template.Name.McsLocalized(), Locales.SUBQUESTESCORTCOMMAND_TARGETNAME, m => BuildWorldEscortMenu(m, mcsBotPlayers, questDatas.Cast<WorldData>()));
            }
            CommandUtils.Apply(EMenuId.QuestEscort.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildAimingMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            foreach (var bodyPartType in Classification.AimingBodyPartTypes)
            {
                var name = Tools.GetBodyPartTypeLocales(bodyPartType).McsLocalized();
                menu.RegisterCommand(name, name, ECommandType.AimingBodyPart.ToString(), mcsBotPlayers, resolver: () => new McsCommandContext { AimingBodyPartType = bodyPartType });
            }

            CommandUtils.Apply(EMenuId.Aiming.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildProxyMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterSubMenu(Locales.QUESTPROXYCOMMAND_NAME, Locales.QUESTPROXYCOMMAND_TARGETNAME, m => BuildQuestProxyMenu(m, mcsBotPlayers));
            menu.RegisterSubMenu(Locales.SWITCHPROXYCOMMAND_NAME, Locales.SWITCHPROXYCOMMAND_TARGETNAME, m => BuildSwitchProxyMenu(m, mcsBotPlayers));

            CommandUtils.Apply(EMenuId.Proxy.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildQuestProxyMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            var questDataMgr = MgrAccessor.Get<QuestDataMgr>();
            if (questDataMgr == null)
            {
                return;
            }

            foreach ((var questDataClass, var questDatas) in questDataMgr.GetQuestDataByGroup())
            {
                menu.RegisterSubMenu(questDataClass.Template.Name.McsLocalized(), Locales.SUBQUESTPROXYCOMMAND_TARGETNAME, m => BuildSubQuestProxyMenu(m, mcsBotPlayers, questDatas));
            }
            CommandUtils.Apply(EMenuId.QuestProxy.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildSubQuestProxyMenu(McsCommandMenu menu, Player[] mcsBotPlayers, List<QuestData> questDatas)
        {
            var myPlayerPos = Singleton<GameWorld>.Instance.MainPlayer.Position;
            foreach (var questData in questDatas)
            {
                menu.RegisterCommand(
                    questData.GetActionName(),
                    questData.GetActionTargetName(myPlayerPos),
                    ECommandType.QuestProxyAction.ToString(), mcsBotPlayers,
                    disabled: questData.IsProxyActionDisabled,
                    resolver: () => new McsCommandContext
                    {
                        Position = questData.GetPos(),
                        TargetId = questData.Id()
                    });
            }
            // 展示内容为任务列表，不进行扩展
        }

        public virtual void BuildSwitchProxyMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            var myPlayerPos = Singleton<GameWorld>.Instance.MainPlayer.Position;
            foreach (var switchData in Gameloop.GetDatas<SwitchData, SwitchDataMgr>())
            {
                menu.RegisterCommand(
                    switchData.GetActionName(),
                    switchData.GetActionTargetName(myPlayerPos),
                    ECommandType.InteractionProxyAction.ToString(), mcsBotPlayers,
                    disabled: switchData.IsProxyActionDisabled,
                    resolver: () => new McsCommandContext
                    {
                        Position = switchData.GetPos(),
                        TargetId = switchData.Id()
                    });
            }
            // 展示内容为开关列表，不进行扩展
        }

        #endregion

        #region Action

        public virtual void ChangeFormationCommandAction(McsCommandContext ctx)
        {
            FormationDataMgr.ApplyFormationData(ctx.TargetId);
        }

        public virtual void OpenInventoryCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
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
                        owner = CommandUtils.GamePlayerOwner,
                        rootItem = rootItem,
                        lootItemOwner = traderControllerClass
                    };
                    inventoryActionClass.method_3();
                }
            }
        }

        public virtual void QuestProxyActionCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
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
                mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup, Decisions.ShouldKeepFormation], Decisions.ShouldQuestProxyAction);
                mcsBotPlayerData.ProxyTargetId = ctx.TargetId;
                mcsBotPlayerData.TargetPos = ctx.Position;
                mcsBotPlayerData.IsLooting = false;
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger,
                });
            }
        }

        public virtual void EscortToWorldPosCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            if (botOwner.Memory.HaveEnemy)
            {
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Negative
                });
            }
            botOwner.Mover.LastTimePosChanged = Time.time;
            botOwner.StopMove();
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup, Decisions.ShouldKeepFormation], Decisions.ShouldEscort);
                mcsBotPlayerData.TargetPos = ctx.Position;
                mcsBotPlayerData.IsLooting = false;
            }
        }

        public virtual void ReportAboutEnemyCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
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
                    PhraseTrigger = EPhraseTrigger.Clear
                });
            }
        }

        public virtual void ReportAboutSelfCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var health = botOwner.HealthController.GetBodyPartHealth(EBodyPart.Common);
            var key1 = $"{(int)health.Current}/{health.Maximum}";
            botOwner.CollectAmmoOrBackupAmmoCount(out var total);
            var key2 = total.ToString();
            var allActiveEffects = botOwner.HealthController.GetAllActiveEffects();
            var healthStates = new List<HealthState>();
            foreach (var activeEffect in allActiveEffects)
            {
                if (Classification.EffectTypeFilter.Contains(activeEffect.Type))
                {
                    continue;
                }

                var effectType = GClass3058.EffectName(activeEffect);
                if (string.IsNullOrEmpty(effectType))
                {
                    continue;
                }

                healthStates.Add(new HealthState
                {
                    BodyPart = activeEffect.BodyPart.ToString(),
                    EffectType = effectType
                });
            }
            var key3 = Json.Serialize(healthStates);
            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Mine,
                Keys = [key1, key2, key3]
            });
        }

        public void ChangeAimingBodyPartCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.AimingBodyPartType = ctx.AimingBodyPartType;
            }
            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Roger
            });
        }

        public void OnYourOwnCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
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
                PhraseTrigger = EPhraseTrigger.Roger
            });
        }

        public virtual void RegroupCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
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
                PhraseTrigger = EPhraseTrigger.Regroup
            });
        }

        public virtual void GoToPointCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            var pos = Tools.GetPosNearTarget(ctx.Position.Value, botOwner);
            if (!pos.HasValue)
            {
                return;
            }

            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Going
            });

            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup, Decisions.ShouldKeepFormation], Decisions.ShouldGoToPoint);
                mcsBotPlayerData.IsLooting = false;
                mcsBotPlayerData.TargetPos = pos.Value;
                mcsBotPlayerData.ProxyTargetId = null;
            }
            botOwner.Mover.LastTimePosChanged = Time.time;
            botOwner.StopMove();
        }

        public virtual void HoldPositionCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            botOwner.StopMove();
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup, Decisions.ShouldKeepFormation], Decisions.ShouldHoldPosition);
                mcsBotPlayerData.IsLooting = false;
                mcsBotPlayerData.TargetPos = null;
                mcsBotPlayerData.ProxyTargetId = null;
            }
            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.HoldPosition
            });
        }

        public virtual void DropTargetLootCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            if (botOwner.ExternalItemsController.HaveItemsToDrop())
            {
                botOwner.StopMove();
                var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                if (mcsBotPlayerData != null)
                {
                    mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup, Decisions.ShouldKeepFormation], Decisions.ShouldDropTargetLoot);
                    mcsBotPlayerData.IsLooting = false;
                    mcsBotPlayerData.TargetPos = null;
                    mcsBotPlayerData.ProxyTargetId = null;
                }
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Roger
                });
            }
            else
            {
                botOwner.TalkMsg(new McsMsg
                {
                    PhraseTrigger = EPhraseTrigger.Negative
                });
            }
        }

        public virtual void ForceTeleportCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer.AIData.BotOwner;
            botOwner.StopMove();
            botOwner.Mover.AllowTeleport();
            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData != null)
            {
                mcsBotPlayerData.SetDecision(null, Decisions.ShouldTeleport);
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
            botOwner.TryResetHandsState();
            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Roger
            });

            if (!MiyakoCarryServicePlugin.SAINInstalled)
            {
                botOwner.Memory.GoalTarget.Clear();
                botOwner.Memory.GoalEnemy = null;
            }
        }

        public virtual void InteractionProxyActionCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
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
                mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup, Decisions.ShouldKeepFormation], Decisions.ShouldInteractionProxyAction);
                var interactableObjectData = Singleton<GameWorld>.Instance.FindInteractableObjectData(ctx.TargetId);
                if (interactableObjectData != null)
                {
                    mcsBotPlayerData.ProxyTargetId = interactableObjectData.Id();
                    mcsBotPlayerData.TargetPos = interactableObjectData.GetPos();
                    botOwner.TalkMsg(new McsMsg
                    {
                        PhraseTrigger = EPhraseTrigger.Roger,
                    });
                }
                mcsBotPlayerData.IsLooting = false;
            }
        }

        public virtual void LootProxyActionCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
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

            if (mcsBotPlayerData == null)
            {
                return;
            }

            var lootData = LootDataMgr.FindLootData(ctx.TargetId);
            mcsBotPlayerData.IsLooting = false;
            mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup, Decisions.ShouldKeepFormation], Decisions.ShouldLootProxyAction);
            LootDataMgr.UnlockLootingTarget(lootData);
            LootDataMgr.UnlockLootingTargetRootTransform(lootData.RootTransform);
            if (!LootDataMgr.IsLockedLootingTarget(lootData) && !LootDataMgr.IsLockedLootingTargetRootTransform(lootData.RootTransform))
            {
                LootDataMgr.LockLootItemToTarget(lootData);
                LootDataMgr.LockLootingTargetRootTransform(lootData.RootTransform);
                mcsBotPlayerData.LootingTarget = lootData;
                mcsBotPlayerData.ProxyTargetId = lootData.Item.Id;
                mcsBotPlayerData.TargetPos = lootData.RootTransform.position;
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

        public virtual void HandleMcsLeadPlayerExtracted(McsLeadPlayerExtractedEvent @event)
        {
            var mcsBotPlayers = McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(@event.McsLeadPlayerId);
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                CommandUtils.Dispatch(
                    ECommandType.GoToExfil.ToString(),
                    [mcsBotPlayer],
                    null
                );
            }
        }

        public virtual void GoToExfilCommandAction(McsCommandContext ctx)
        {
            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer?.AIData?.BotOwner;
            if (botOwner == null)
            {
                return;
            }

            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }

            mcsBotPlayerData.SetDecision(null, Decisions.ShouldExfil);
        }

        public virtual void ClearAreaCommandAction(McsCommandContext ctx)
        {
            if (!ctx.Position.HasValue)
            {
                return;
            }

            var mcsBotPlayer = ctx.McsBotPlayer;
            var botOwner = mcsBotPlayer?.AIData?.BotOwner;
            if (botOwner == null)
            {
                return;
            }

            var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }
            var center = ctx.Position.Value;
            var leadId = ctx.McsLeadPlayer.ProfileId;
            var mcsAILeadPlayer = mcsBotPlayerData.McsAILeadPlayer;
            if (mcsAILeadPlayer == null)
            {
                return;
            }

            List<Player> members;
            List<List<Vector3>> segments;

            if (mcsAILeadPlayer.ClearAreaCacheCenter == center
                && Time.time - mcsAILeadPlayer.ClearAreaCacheTime < 1f
                && mcsAILeadPlayer.ClearAreaCacheMembers != null
                && mcsAILeadPlayer.ClearAreaCacheSegments != null)
            {
                members = mcsAILeadPlayer.ClearAreaCacheMembers;
                segments = mcsAILeadPlayer.ClearAreaCacheSegments;
            }
            else
            {
                members = MgrAccessor.Get<McsMgr>()
                    .GetAllAliveMcsSquadMembersByMcsLeadId(leadId)
                    .Where(p => p != null).OrderBy(p => p.ProfileId).ToList();

                if (members.Count == 0)
                {
                    return;
                }

                var startFrom = members[0].Position;
                var fullRoute = CommandUtils.GenerateClearAreaWaypoints(center, 30f, startFrom);
                if (fullRoute.Count == 0)
                {
                    return;
                }

                segments = CommandUtils.SplitRoute(fullRoute, members.Count);

                mcsAILeadPlayer.ClearAreaCacheCenter = center;
                mcsAILeadPlayer.ClearAreaCacheTime = Time.time;
                mcsAILeadPlayer.ClearAreaCacheMembers = members;
                mcsAILeadPlayer.ClearAreaCacheSegments = segments;
            }

            var total = members.Count;
            var index = members.FindIndex(p => p.ProfileId == ctx.McsBotPlayer.ProfileId);
            if (total == 0 || index < 0)
            {
                return;
            }

            var seg = new List<Vector3>(segments[index]);
            if (seg.Count == 0)
            {
                return;
            }

            if (seg[seg.Count - 1].McsSqrDistance(botOwner.Position) < seg[0].McsSqrDistance(botOwner.Position))
            {
                seg.Reverse();
            }

            botOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.Going
            });
            mcsBotPlayerData.ClearAreaPoints = seg;
            mcsBotPlayerData.ClearAreaIndex = 0;
            mcsBotPlayerData.ClearAreaLookAroundUntil = 0f;
            mcsBotPlayerData.TargetPos = seg[0];
            mcsBotPlayerData.IsLooting = false;
            mcsBotPlayerData.ProxyTargetId = null;
            mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup, Decisions.ShouldKeepFormation], Decisions.ShouldClearArea);
            botOwner.Mover.LastTimePosChanged = Time.time;
            botOwner.StopMove();
        }

        #endregion
    }
}