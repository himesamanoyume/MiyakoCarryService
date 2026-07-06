using System;
using System.Collections.Generic;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    public delegate void McsCommandAction(Player[] mcsBotPlayers, params object[] args);

    public sealed class McsCommandMenu
    {
        public readonly List<McsCommandEntry> Entries = new();

        public McsCommandMenu RegisterCommand(
            string name, string targetName,
            McsCommandAction action, Player[] mcsBotPlayers,
            Func<bool> disabled = null, params object[] args)
        {
            Entries.Add(new McsCommandEntry
            {
                Name = name,
                TargetName = targetName,
                DisabledPredicate = disabled,
                Action = action,
                McsBotPlayers = mcsBotPlayers,
                Args = args ?? Array.Empty<object>()
            });
            return this;
        }

        public McsCommandMenu RegisterSubMenu(
            string name, string targetName,
            Action<McsCommandMenu> build, Func<bool> disabled = null)
        {
            Entries.Add(new McsCommandEntry
            {
                Name = name,
                TargetName = targetName,
                BuildSubMenu = build,
                DisabledPredicate = disabled
            });
            return this;
        }

        public sealed class McsCommandEntry
        {
            public string Name;
            public string TargetName;
            public Func<bool> DisabledPredicate;
            public bool Disabled => DisabledPredicate?.Invoke() ?? false;

            public McsCommandAction Action;
            public Player[] McsBotPlayers;
            public object[] Args;

            public Action<McsCommandMenu> BuildSubMenu;

            public bool IsSubMenu => BuildSubMenu != null;
        }
    }
}