using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using UnityEngine;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Interfaces;
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
        }

        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        private LootDataMgr LootDataMgr => MgrAccessor.Get<LootDataMgr>();

        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            CommandUtils.ClearGamePlayerOwner();
            CommandUtils.ClearMenuStack();
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            CommandUtils.ClearGamePlayerOwner();
            CommandUtils.ClearMenuStack();
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

            // 不打算对根菜单进行扩展
            // CommandUtils.Apply(EMenuId.Main.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildTeamMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterCommand(Locales.TEAMREPORTABOUTENEMYCOMMAND_NAME, Locales.TEAMREPORTABOUTENEMYCOMMAND_TARGETNAME, ReportAboutEnemyCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMREPORTABOUTSELFCOMMAND_NAME, Locales.TEAMREPORTABOUTSELFCOMMAND_TARGETNAME, ReportAboutSelfCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMONYOUROWNCOMMAND_NAME, Locales.TEAMONYOUROWNCOMMAND_TARGETNAME, OnYourOwnCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMREGROUPCOMMAND_NAME, Locales.TEAMREGROUPCOMMAND_TARGETNAME, RegroupCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMGOTOPOINTCOMMAND_NAME, Locales.TEAMGOTOPOINTCOMMAND_TARGETNAME, GoToPointCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMHOLDPOSITIONCOMMAND_NAME, Locales.TEAMHOLDPOSITIONCOMMAND_TARGETNAME, HoldPositionCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.TEAMDROPTARGETLOOTCOMMAND_NAME, Locales.TEAMDROPTARGETLOOTCOMMAND_TARGETNAME, DropTargetLootCommandAction, mcsBotPlayers);
            menu.RegisterSubMenu(Locales.TEAMESCORTCOMMAND_NAME, Locales.TEAMESCORTCOMMAND_TARGETNAME, m => BuildEscortMenu(m, mcsBotPlayers, true));
            menu.RegisterSubMenu(Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.TEAMCHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME, m => BuildAimingMenu(m, mcsBotPlayers));
            menu.RegisterCommand(Locales.TEAMFORCETELEPORTCOMMAND_NAME, Locales.TEAMFORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction, mcsBotPlayers);

            CommandUtils.Apply(EMenuId.Team.ToString(), menu, mcsBotPlayers);
        }

        public virtual void BuildMemberMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterCommand(Locales.REPORTABOUTENEMYCOMMAND_NAME, Locales.REPORTABOUTENEMYCOMMAND_TARGETNAME, ReportAboutEnemyCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.REPORTABOUTSELFCOMMAND_NAME, Locales.REPORTABOUTSELFCOMMAND_TARGETNAME, ReportAboutSelfCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.ONYOUROWNCOMMAND_NAME, Locales.ONYOUROWNCOMMAND_TARGETNAME, OnYourOwnCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.REGROUPCOMMAND_NAME, Locales.REGROUPCOMMAND_TARGETNAME, RegroupCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.GOTOPOINTCOMMAND_NAME, Locales.GOTOPOINTCOMMAND_TARGETNAME, GoToPointCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.HOLDPOSITIONCOMMAND_NAME, Locales.HOLDPOSITIONCOMMAND_TARGETNAME, HoldPositionCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.DROPTARGETLOOTCOMMAND_NAME, Locales.DROPTARGETLOOTCOMMAND_TARGETNAME, DropTargetLootCommandAction, mcsBotPlayers);
            menu.RegisterCommand(Locales.OPENINVENTORYCOMMAND_NAME, Locales.OPENINVENTORYCOMMAND_TARGETNAME, OpenInventoryCommandAction, mcsBotPlayers, disabled: () => MiyakoCarryServicePlugin.McsPluginClientConfig.BalanceRestriction);
            menu.RegisterSubMenu(Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_NAME, Locales.CHANGEAIMINGBODYPARTTYPECOMMAND_TARGETNAME, m => BuildAimingMenu(m, mcsBotPlayers));
            menu.RegisterSubMenu(Locales.ESCORTCOMMAND_NAME, Locales.ESCORTCOMMAND_TARGETNAME, m => BuildEscortMenu(m, mcsBotPlayers, false));
            menu.RegisterSubMenu(Locales.PROXYCOMMAND_NAME, Locales.PROXYCOMMAND_TARGETNAME, m => BuildProxyMenu(m, mcsBotPlayers));
            menu.RegisterCommand(Locales.FORCETELEPORTCOMMAND_NAME, Locales.FORCETELEPORTCOMMAND_TARGETNAME, ForceTeleportCommandAction, mcsBotPlayers);

            CommandUtils.Apply(EMenuId.Member.ToString(), menu, mcsBotPlayers);
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
                    EscortToWorldPosCommandAction, mcsBotPlayers,
                    disabled: worldData.IsDisabled,
                    args: worldData);
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
                menu.RegisterCommand(name, name, ChangeAimingBodyPartCommandAction, mcsBotPlayers, args: bodyPartType);
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
                    QuestProxyActionCommandAction, mcsBotPlayers,
                    disabled: questData.IsProxyActionDisabled,
                    args: questData);
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
                    InteractionProxyActionCommandAction, mcsBotPlayers,
                    disabled: switchData.IsProxyActionDisabled,
                    args: switchData);
            }
            // 展示内容为开关列表，不进行扩展
        }

        #endregion

        #region Action

        public virtual void OpenInventoryCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
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
                            owner = CommandUtils.GamePlayerOwner,
                            rootItem = rootItem,
                            lootItemOwner = traderControllerClass
                        };
                        inventoryActionClass.method_3();
                    }
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void QuestProxyActionCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            var questData = (QuestData)args[0];
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.QuestProxyAction.ToString(),
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
                        mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldQuestProxyAction);
                        mcsBotPlayerData.ProxyTargetId = questData.Id();
                        mcsBotPlayerData.TargetPos = questData.GetPos();
                        mcsBotPlayerData.IsLooting = false;
                        botOwner.TalkMsg(new McsMsg
                        {
                            PhraseTrigger = EPhraseTrigger.Roger,
                        });
                    }
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void ForEachAlive(Player[] mcsBotPlayers, Action<Player> action)
        {
            foreach (var player in mcsBotPlayers)
            {
                if (player == null || !player.HealthController.IsAlive)
                {
                    continue;
                }
                action(player);
            }
        }

        public virtual void EscortToWorldPosCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            var worldData = (WorldData)args[0];
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.Escort.ToString(),
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
                            PhraseTrigger = EPhraseTrigger.Negative
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
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void ReportAboutEnemyCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.ReportAboutEnemy.ToString()
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
                            PhraseTrigger = EPhraseTrigger.Clear
                        });
                    }
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void ReportAboutSelfCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.ReportAboutSelf.ToString()
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
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public void ChangeAimingBodyPartCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            var aimingBodyPartType = (BodyPartType)args[0];
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.AimingBodyPart.ToString(),
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
                        PhraseTrigger = EPhraseTrigger.Roger
                    });
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public void OnYourOwnCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.OnYourOwn.ToString()
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
                        PhraseTrigger = EPhraseTrigger.Roger
                    });
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void RegroupCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.Regroup.ToString()
                    });
                }
                else
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
                        PhraseTrigger = EPhraseTrigger.Regroup
                    });
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void GoToPointCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    if (Physics.Raycast(Singleton<GameWorld>.Instance.MainPlayer.InteractionRay, out var raycastHit, float.MaxValue, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        EventMgr.Notify(new CommandMgrHandleFikaEvent
                        {
                            McsBotPlayer = mcsBotPlayer,
                            CommandPacketType = ECommandPacketType.GoToPoint.ToString(),
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
                                PhraseTrigger = EPhraseTrigger.Going
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
                        }
                    }
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void HoldPositionCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.HoldPosition.ToString()
                    });
                }
                else
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
                        PhraseTrigger = EPhraseTrigger.HoldPosition
                    });
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void DropTargetLootCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.DropTargetLoot.ToString()
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
                            mcsBotPlayerData.SetDecision([Decisions.ShouldRegroup], Decisions.ShouldDropTargetLoot);
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
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void ForceTeleportCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.Teleport.ToString()
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
                        PhraseTrigger = EPhraseTrigger.Roger
                    });

                    if (!MiyakoCarryServicePlugin.SAINInstalled)
                    {
                        botOwner.Memory.GoalTarget.Clear();
                        botOwner.Memory.GoalEnemy = null;
                    }
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void InteractionProxyActionCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            var proxyAction = (IProxyActor)args[0];
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.InteractionProxyAction.ToString(),
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
                }
            });
            CommandUtils.CloseCommandMenuAction();
        }

        public virtual void LootProxyActionCommandAction(Player[] mcsBotPlayers, params object[] args)
        {
            var lootData = (LootData)args[0];
            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                if (MiyakoCarryServicePlugin.FikaInstalled && !Tools.IsHost)
                {
                    EventMgr.Notify(new CommandMgrHandleFikaEvent
                    {
                        McsBotPlayer = mcsBotPlayer,
                        CommandPacketType = ECommandPacketType.LootProxyAction.ToString(),
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
                }
            });
            CommandUtils.CloseCommandMenuAction();
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
                        CommandPacketType = ECommandPacketType.GoToExfil.ToString()
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

        #endregion
    }
}