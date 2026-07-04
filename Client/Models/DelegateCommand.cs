
using System;
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Interfaces;

namespace MiyakoCarryService.Client.Models
{
    public sealed class DelegateCommand : ICommand
    {
        private readonly Func<Player, bool> _disabled;
        private readonly Action<Player> _execute;
        private readonly Action<List<Player>> _executeTeam;

        public string NameKey { get; }
        public string TargetNameKey { get; }
        public ECommandScope Scope { get; }
        public bool IsSubMenu { get; }

        public DelegateCommand(string name, string target, ECommandScope scope,
            bool isSubMenu, Func<Player, bool> disabled,
            Action<Player> execute, Action<List<Player>> executeTeam = null)
        {
            NameKey = name;
            TargetNameKey = target;
            Scope = scope;
            IsSubMenu = isSubMenu;
            _disabled = disabled;
            _execute = execute;
            _executeTeam = executeTeam;
        }

        public bool IsDisabled(Player p) => _disabled?.Invoke(p) ?? false;
        public void Execute(Player p) => _execute?.Invoke(p);
        public void ExecuteTeam(List<Player> players) => _executeTeam?.Invoke(players);
    }
}