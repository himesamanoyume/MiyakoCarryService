using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using TMPro;

namespace MiyakoCarryService.Client.Utils
{
    internal static class CommandUtils
    {
        private static readonly Dictionary<string, List<Action<McsCommandMenu, Player[]>>> _extensions = new();
        private static readonly Stack<Action<McsCommandMenu>> _menuStack = new();
        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        public static void ClearGamePlayerOwner()
        {
            GamePlayerOwner = null;
        }

        public static void ClearMenuStack()
        {
            _menuStack.Clear();
        }

        public static void Register(string menuKey, Action<McsCommandMenu, Player[]> menu)
        {
            if (string.IsNullOrEmpty(menuKey) || menu == null)
            {
                return;
            }

            if (!_extensions.TryGetValue(menuKey, out var action))
            {
                action = new List<Action<McsCommandMenu, Player[]>>();
                _extensions[menuKey] = action;
            }
            action.Add(menu);
        }

        public static void Apply(string menuKey, McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            if (_extensions.TryGetValue(menuKey, out var actions))
            {
                foreach (var action in actions)
                {
                    action(menu, mcsBotPlayers);
                }
            }
        }

        public static void PreBuildCommandMenu(out ActionsReturnClass actionsReturnClass)
        {
            actionsReturnClass = new ActionsReturnClass
            {
                Actions = new()
            };

            actionsReturnClass.CurrentActionChanged.Bind(OnCurrentActionChanged);
        }

        public static void OnCurrentActionChanged()
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

        public static void PostBuildCommandMenu(ActionsReturnClass actionsReturnClass)
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

        public static void CloseCommandMenuAction()
        {
            _menuStack.Clear();
            GamePlayerOwner.AvailableInteractionState.Value = new ActionsReturnClass();
        }

        public static ActionsTypesClass MakeCommand(string name, string targetName, bool disabled, Action action)
        {
            return new ActionsTypesClass { Name = name, TargetName = targetName, Disabled = disabled, Action = action };
        }

        public static GamePlayerOwner GamePlayerOwner
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

        public static void OpenMenu(Action<McsCommandMenu> builder)
        {
            _menuStack.Push(builder);
            RenderMenu();
        }

        private static void BackToParentMenu()
        {
            if (_menuStack.Count > 1)
            {
                _menuStack.Pop();
            }
            RenderMenu();
        }

        private static void RenderMenu()
        {
            if (GamePlayerOwner == null || _menuStack.Count == 0)
            {
                return;
            }

            var builder = _menuStack.Peek();
            var menu = new McsCommandMenu();
            builder(menu);

            PreBuildCommandMenu(out var actionsReturnClass);

            if (_menuStack.Count > 1)
            {
                actionsReturnClass.Actions.Add(MakeCommand(Locales.BACKCOMMAND_NAME, Locales.BACKCOMMAND_TARGETNAME, false, BackToParentMenu));
            }

            foreach (var entry in menu.Entries)
            {
                if (entry.IsSubMenu)
                {
                    actionsReturnClass.Actions.Add(MakeCommand(entry.Name, entry.TargetName, entry.Disabled, () => OpenMenu(entry.BuildSubMenu)));
                }
                else
                {
                    actionsReturnClass.Actions.Add(MakeCommand(entry.Name, entry.TargetName, entry.Disabled, () => entry.Action?.Invoke(entry.McsBotPlayers, entry.Args)));
                }
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public static Player[] GetAliveMembers()
        {
            return McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(Singleton<GameWorld>.Instance.MainPlayer.ProfileId).ToArray();
        }
    }
}