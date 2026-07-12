using System;
using System.Collections.Generic;
using EFT;

namespace MiyakoCarryService.Client.Models
{
    public delegate McsCommandContext McsCommandResolver();  

    public sealed class McsCommandMenu
    {
        public readonly List<McsCommandEntry> Entries = new();

        public McsCommandMenu RegisterCommand(
            string name, string targetName, 
            string commandType, Player[] mcsBotPlayers,
            Func<bool> disabled = null, McsCommandResolver resolver = null)
        {
            Entries.Add(new McsCommandEntry
            {
                Name = name,
                TargetName = targetName,
                DisabledPredicate = disabled,
                CommandType = commandType,
                McsBotPlayers = mcsBotPlayers,
                Resolver = resolver,  
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

            public string CommandType;
            public Player[] McsBotPlayers;
            public McsCommandResolver Resolver;

            public Action<McsCommandMenu> BuildSubMenu;

            public bool IsSubMenu => BuildSubMenu != null;
        }
    }
}