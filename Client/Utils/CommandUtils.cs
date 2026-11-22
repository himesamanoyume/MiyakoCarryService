using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Comfort.Common;
using EFT;
using EFT.UI;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Utils
{
    internal static class CommandUtils
    {
        private static readonly Dictionary<string, List<Action<McsCommandMenu, Player[]>>> _extensions = new();
        private static readonly Stack<Action<McsCommandMenu>> _menuStack = new();
        private static AvailableInteractionState _currentMenu;
        private static InteractionAction _cancelAction;
        private static readonly List<InteractionAction> _injectedObjectActions = new();
        private static readonly Stack<int> _selectionStack = new();
        private static int _pendingSelectedIndex = -1;

        private static string _lastInjectedSignature;

        private static readonly HashSet<IntPtr> _ourMenuPointers = new();

        public static bool IsCommandMenuOpen => _menuStack.Count > 0;
        public static AvailableInteractionState CurrentMenu => _currentMenu;

        public static bool IsCurrentMenu(AvailableInteractionState state)
        {
            if (state == null || _currentMenu == null)
            {
                return false;
            }

            return state.Pointer == _currentMenu.Pointer;
        }

        public static bool IsOurMenu(AvailableInteractionState state)
        {
            if (state == null)
            {
                return false;
            }

            return _ourMenuPointers.Contains(state.Pointer);
        }

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();
        private static Vector3[] _cachedVertices;
        private static bool _navMeshUnavailable;

        private static ConcurrentDictionary<string, McsCommandHandler> _handlersMap;

        public static void RegisterCommandHandler(string commandTypeName, McsCommandHandler handler)
        {
            if (_handlersMap == null)
            {
                _handlersMap = new();
            }

            _handlersMap.AddOrUpdate(commandTypeName, handler,
                (_commandPacketTypeName, oldHandler) =>
                {
                    oldHandler = handler;
                    return oldHandler;
                }
            );
        }

        public static void Execute(McsCommandContext ctx, bool shouldCheckData)
        {
            if (ctx?.CommandType == null)
            {
                return;
            }

            if (shouldCheckData)
            {
                var mcsBotPlayer = ctx.McsBotPlayer;
                var mcsBotPlayerData = mcsBotPlayer.AIData.BotOwner.GetMcsBotPlayerData();
                if (ctx.ShouldCheckExclude && mcsBotPlayerData.IsExcluded)
                {
                    return;
                }
            }

            if (_handlersMap.TryGetValue(ctx.CommandType, out var handler))
            {
                handler(ctx);
            }
        }

        public static void ClearGamePlayerOwner()
        {
            GamePlayerOwner = null;
        }

        public static void NavMeshCache()
        {
            TryFillCachedVertices();
        }

        public static void ClearNavMeshCache()
        {
            _cachedVertices = null;
            _navMeshUnavailable = false;
        }

        private static void TryFillCachedVertices()
        {
            if (_cachedVertices != null || _navMeshUnavailable)
            {
                return;
            }

            if (NavMeshTriangulationIcall.TryGetVertices(out var vertices))
            {
                _cachedVertices = vertices;
                return;
            }

            _navMeshUnavailable = true;
        }

        public static void ClearMenuStack()
        {
            _menuStack.Clear();
            _selectionStack.Clear();
            _pendingSelectedIndex = -1;
            _ourMenuPointers.Clear();
        }

        public static void RegisterCommandMenu(string menuKey, Action<McsCommandMenu, Player[]> menu)
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

        public static void PreBuildCommandMenu(out AvailableInteractionState actionsReturnClass)
        {
            actionsReturnClass = new AvailableInteractionState
            {
                Actions = new()
            };

            _ourMenuPointers.Add(actionsReturnClass.Pointer);

            actionsReturnClass.CurrentActionChanged.Bind(new Action(OnCurrentActionChanged));
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

            var itemName = actionPanel._itemName;

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

        public static void PostBuildCommandMenu(AvailableInteractionState actionsReturnClass)
        {
            if (GamePlayerOwner == null)
            {
                return;
            }

            if (actionsReturnClass != null)
            {
                _cancelAction = MakeCommand(Locales.CANCELCOMMAND_NAME, Locales.CANCELCOMMAND_TARGETNAME, false, CloseCommandMenuAction);
                actionsReturnClass.Actions.Add(_cancelAction);
            }

            var existing = GamePlayerOwner.AvailableInteractionState.Value;
            var previousCommandMenu = _currentMenu;

            _currentMenu = actionsReturnClass;
            _injectedObjectActions.Clear();
            _lastInjectedSignature = null;

            if (existing != null && !ReferenceEquals(existing, previousCommandMenu) && existing.Actions != null && existing.Actions.Count > 0)
            {
                MergeIncomingIntoCommandMenu(existing);
            }

            if (_currentMenu != null)
            {
                if (_pendingSelectedIndex >= 0 && _pendingSelectedIndex < _currentMenu.Actions.Count && !_currentMenu.Actions[_pendingSelectedIndex].Disabled)
                {
                    _currentMenu.SelectAction(_currentMenu.Actions[_pendingSelectedIndex]);
                }
                else
                {
                    SelectFirstAvailableAction();
                }
            }

            _pendingSelectedIndex = -1;
            GamePlayerOwner.AvailableInteractionState.Value = _currentMenu;
        }

        public static void CloseCommandMenuAction()
        {
            _menuStack.Clear();
            _selectionStack.Clear();
            _pendingSelectedIndex = -1;
            _lastInjectedSignature = null;
            _currentMenu = null;
            _cancelAction = null;
            _injectedObjectActions.Clear();
            GamePlayerOwner.AvailableInteractionState.Value = new AvailableInteractionState();
        }

        public static void KeepCommandMenuOpen()
        {
            try
            {
                if (!IsCommandMenuOpen)
                {
                    return;
                }

                if (!Singleton<GameWorld>.Instantiated || Singleton<GameWorld>.Instance?.MainPlayer == null)
                {
                    return;
                }

                var owner = GamePlayerOwner;
                if (owner == null)
                {
                    return;
                }

                var bindable = owner.AvailableInteractionState;
                if (bindable == null)
                {
                    return;
                }

                var currentState = bindable.Value;
                if (ReferenceEquals(currentState, _currentMenu))
                {
                    return;
                }

                MergeIncomingIntoCommandMenu(currentState);
                bindable.Value = _currentMenu;
            }
            catch (Exception ex)
            {
                McsLogger.LogError($"[CommandUtils.KeepCommandMenuOpen] {ex}");
            }
        }

        public static void OnGameProducedInteractionState(GamePlayerOwner owner)
        {
            if (!IsCommandMenuOpen || _currentMenu == null || owner == null)
            {
                return;
            }

            var bindable = owner.AvailableInteractionState;
            if (bindable == null)
            {
                return;
            }

            var currentState = bindable.Value;
            if (ReferenceEquals(currentState, _currentMenu))
            {
                return;
            }

            MergeIncomingIntoCommandMenu(currentState);
            bindable.Value = _currentMenu;
        }

        public static void ReassertCommandMenu(GamePlayerOwner owner)
        {
            if (!IsCommandMenuOpen || _currentMenu == null || owner == null)
            {
                return;
            }

            var bindable = owner.AvailableInteractionState;
            if (bindable == null)
            {
                return;
            }

            if (ReferenceEquals(bindable.Value, _currentMenu))
            {
                return;
            }

            bindable.Value = _currentMenu;
        }

        private static void SelectFirstAvailableAction()
        {
            if (_currentMenu?.Actions == null)
            {
                return;
            }

            foreach (var action in _currentMenu.Actions)
            {
                if (action == null || action.Disabled)
                {
                    continue;
                }

                _currentMenu.SelectAction(action);
                return;
            }

            _currentMenu.InitSelected();
        }

        public static InteractionAction MakeCommand(string name, string targetName, bool disabled, Action action)
        {
            return new InteractionAction { Name = name, TargetName = targetName, Disabled = disabled, Action = action };
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
            if (_menuStack.Count > 0 && _currentMenu != null)
            {
                var index = _currentMenu.SelectedAction != null ? _currentMenu.Actions.IndexOf(_currentMenu.SelectedAction) : -1;
                _selectionStack.Push(index);
            }

            _menuStack.Push(builder);
            RenderMenu();
        }

        private static void BackToParentMenu()
        {
            if (_menuStack.Count > 1)
            {
                _menuStack.Pop();
                _pendingSelectedIndex = _selectionStack.Count > 0 ? _selectionStack.Pop() : -1;
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
                    actionsReturnClass.Actions.Add(MakeCommand(entry.Name, entry.TargetName, entry.Disabled, () => Dispatch(entry.CommandType, entry.McsBotPlayers, entry.Resolver, entry.IsLocal, entry.ShouldCheckExclude)));
                }
            }

            PostBuildCommandMenu(actionsReturnClass);
        }

        public static Player[] GetAliveMembers()
        {
            return McsMgr.GetAllAliveMcsSquadMembersByMcsLeadId(Singleton<GameWorld>.Instance.MainPlayer.ProfileId).ToArray();
        }

        public static void Dispatch(string commandType, Player[] mcsBotPlayers, McsCommandResolver resolver, bool isLocal = false, bool shouldCheckExclude = false)
        {
            var data = resolver?.Invoke();
            if (resolver != null && data == null)
            {
                CloseCommandMenuAction();
                return;
            }

            ForEachAlive(mcsBotPlayers, mcsBotPlayer =>
            {
                var ctx = CreateCommandContext(commandType, mcsBotPlayer, data, shouldCheckExclude);
                ctx.McsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                Execute(ctx, true);
            });

            if (isLocal && (mcsBotPlayers == null || mcsBotPlayers.Length == 0))
            {
                var ctx = CreateCommandContext(commandType, null, data, false);
                ctx.McsLeadPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                Execute(ctx, false);
            }

            CloseCommandMenuAction();
        }

        private static McsCommandContext CreateCommandContext( string commandType, Player mcsBotPlayer, McsCommandContext data, bool shouldCheckExclude)
        {
            return new McsCommandContext
            {
                CommandType = commandType,
                Position = data?.Position,
                TargetId = data?.TargetId,
                AimingBodyPartType = data?.AimingBodyPartType ?? default,
                Extensions = data?.Extensions ?? new(),
                ShouldCheckExclude = data?.ShouldCheckExclude ?? shouldCheckExclude,
                McsBotPlayer = mcsBotPlayer
            };
        }

        public static void ForEachAlive(Player[] mcsBotPlayers, Action<Player> action)
        {
            if (mcsBotPlayers == null)
            {
                return;
            }

            foreach (var player in mcsBotPlayers)
            {
                if (player == null || !player.HealthController.IsAlive)
                {
                    continue;
                }
                action(player);
            }
        }

        public static List<Vector3> GenerateClearAreaWaypoints(
            Vector3 center, float radius, Vector3 startFrom,
            float cellSize = 4f, float floorHeight = 3f, float verticalWeight = 8f)
        {
            var result = new List<Vector3>();

            TryFillCachedVertices();
            var verts = _cachedVertices;
            if (verts == null || verts.Length == 0)
            {
                return result;
            }

            var sqrRadius = radius * radius;

            var buckets = new Dictionary<(int, int, int), Vector3>();
            foreach (var v in verts)
            {
                var dx = v.x - center.x;
                var dz = v.z - center.z;
                if (dx * dx + dz * dz > sqrRadius)
                {
                    continue;
                }
                var key = (
                    Mathf.FloorToInt(v.x / cellSize),
                    Mathf.FloorToInt(v.y / floorHeight),
                    Mathf.FloorToInt(v.z / cellSize)
                );
                if (!buckets.ContainsKey(key))
                {
                    buckets[key] = v;
                }
            }
            if (buckets.Count == 0)
            {
                return result;
            }

            var candidates = new List<Vector3>();
            foreach (var p in buckets.Values)
            {
                if (NavMesh.SamplePosition(p, out var hit, 1.5f, NavMesh.AllAreas))
                {
                    var path = new NavMeshPath();
                    if (NavMesh.CalculatePath(startFrom, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        candidates.Add(hit.position);
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return result;
            }

            var remaining = new List<Vector3>(candidates);
            var current = startFrom;
            while (remaining.Count > 0)
            {
                int best = 0;
                float bestCost = float.MaxValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    var dx = remaining[i].x - current.x;
                    var dy = remaining[i].y - current.y;
                    var dz = remaining[i].z - current.z;
                    var cost = dx * dx + dz * dz + verticalWeight * dy * dy;
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = i;
                    }
                }
                current = remaining[best];
                result.Add(current);
                remaining.RemoveAt(best);
            }

            return result;
        }

        public static List<List<Vector3>> SplitRoute(List<Vector3> fullRoute, int count)
        {
            var segments = new List<List<Vector3>>();
            if (count <= 0)
            {
                return segments;
            }

            for (var i = 0; i < count; i++)
            {
                segments.Add(new List<Vector3>());
            }

            if (fullRoute == null || fullRoute.Count == 0)
            {
                return segments;
            }

            var total = fullRoute.Count;
            var baseSize = total / count;
            var remainder = total % count;

            var idx = 0;
            for (int i = 0; i < count; i++)
            {
                var take = baseSize + (i < remainder ? 1 : 0);
                for (var j = 0; j < take && idx < total; j++)
                {
                    segments[i].Add(fullRoute[idx++]);
                }
            }

            return segments;
        }

        public static void MergeIncomingIntoCommandMenu(AvailableInteractionState incoming)
        {
            if (_currentMenu == null)
            {
                return;
            }

            if (incoming != null && _ourMenuPointers.Contains(incoming.Pointer))
            {
                return;
            }

            var hasIncoming = incoming != null && !ReferenceEquals(incoming, _currentMenu) && incoming.Actions != null && incoming.Actions.Count > 0;

            List<InteractionAction> incomingActions = null;
            if (hasIncoming)
            {
                incomingActions = [.. incoming.Actions];

                if (BuildActionSignature(incomingActions) == _lastInjectedSignature)
                {
                    return;
                }
            }

            foreach (var action in _injectedObjectActions)
            {
                _currentMenu.Actions.Remove(action);
            }
            _injectedObjectActions.Clear();

            if (hasIncoming)
            {
                var insertIndex = _cancelAction != null ? _currentMenu.Actions.IndexOf(_cancelAction) : -1;
                if (insertIndex < 0)
                {
                    insertIndex = _currentMenu.Actions.Count;
                }

                foreach (var action in incomingActions)
                {
                    _currentMenu.Actions.Insert(insertIndex++, action);
                    _injectedObjectActions.Add(action);
                }

                _lastInjectedSignature = BuildActionSignature(incomingActions);
            }
            else
            {
                _lastInjectedSignature = null;
            }

            var prev = _currentMenu.SelectedAction;
            if (prev == null || prev.Disabled || !_currentMenu.Actions.Contains(prev))
            {
                SelectFirstAvailableAction();
            }
        }

        private static string BuildActionSignature(List<InteractionAction> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var action in actions)
            {
                if (action == null)
                {
                    continue;
                }

                builder.Append(action.Name).Append('|').Append(action.TargetName).Append(';');
            }

            return builder.ToString();
        }
    }
}
