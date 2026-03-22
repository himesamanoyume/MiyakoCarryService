

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class CommandMgr : BaseMgr<CommandMgr>
    {
        public sealed override void Start()
        {
            base.Start();
        }

        private GamePlayerOwner _gamePlayerOwner
        {
            get
            {
                return field ??= Singleton<GameWorld>.Instance.MainPlayer.GetComponentInChildren<GamePlayerOwner>();
            }
        }

        private List<MongoID> _mcsBotPlayerIds = new();
        private ConcurrentDictionary<MongoID, Player> _mcsBotPlayers = new();
        private bool _commandMenuOpening = false;

        protected sealed override void OnRaidStarted()
        {
            base.OnRaidStarted();
            _commandMenuOpening = false;
            _mcsBotPlayerIds = McsRequestHandler.GetMcsBotPlayerIds().GetAwaiter().GetResult();
        }

        protected sealed override void OnRaidEnded()
        {
            base.OnRaidEnded();
            _commandMenuOpening = false;
            _mcsBotPlayerIds.Clear();
            _mcsBotPlayers.Clear();
        }

        void Update()
        {
            // 监听快捷键（长按）
            if (!_gameloop.IsVaildGameWorld)
            {
                return;
            }

            if (KeyInput.BetterIsPressed(MiyakoCarryServicePlugin.CommandHotKey.Value))
            {
                if (!_commandMenuOpening)
                {
                    _commandMenuOpening = true;
                    BuildMainCommandMenu();
                }
            }
            else
            {
                if (_commandMenuOpening)
                {
                    _commandMenuOpening = false;
                    CloseCommandMenu();
                }
            }
            // end
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

            actionsReturnClass.Actions.Add(TeamCommand(BuildTeamCommandMenu));

            foreach (var mcsBotPlayerId in _mcsBotPlayerIds)
            {
                var mcsBotPlayer = TryGetMcsBotPlayer(mcsBotPlayerId);
                if (mcsBotPlayer == null)
                {
                    continue;
                }

                actionsReturnClass.Actions.Add(MemberCommand(BuildMemberCommandMenu, mcsBotPlayer));
            }

            actionsReturnClass.Actions.Add(CancelCommand(CloseCommandMenu));

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
            
            actionsReturnClass.Actions.Add(TeamHoldCommand(null));
            actionsReturnClass.Actions.Add(CancelCommand(CloseCommandMenu));

            if (actionsReturnClass != null)
            {
                actionsReturnClass.InitSelected();
            }

            _gamePlayerOwner.AvailableInteractionState.Value = actionsReturnClass;
        }

        public ActionsTypesClass TeamCommand(Action action)
        {
            return new ActionsTypesClass
            {
                Name = "全体听令",
                TargetName = "对所有护航成员发号施令",
                Disabled = false,
                Action = action
            };
        }

        public ActionsTypesClass MemberCommand(Action<Player> action, Player mcsBotPlayer)
        {
            return new ActionsTypesClass
            {
                Name = $"{mcsBotPlayer.Profile.Info.Nickname} 听令",
                TargetName = "单独给一个护航下达指令",
                Disabled = mcsBotPlayer.ActiveHealthController.IsAlive,
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


            actionsReturnClass.Actions.Add(HoldCommand(null));

            actionsReturnClass.Actions.Add(CancelCommand(CloseCommandMenu));

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

        public ActionsTypesClass CancelCommand(Action action)
        {
            return new ActionsTypesClass
            {
                Name = "取消",
                TargetName = "取消下达指令",
                Disabled = false,
                Action = action
            };
        }

        public void CloseCommandMenu()
        {
            _gamePlayerOwner.AvailableInteractionState.Value = new ActionsReturnClass();
            _commandMenuOpening = false;
        }
    }
}