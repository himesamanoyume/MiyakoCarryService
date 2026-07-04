
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Interfaces
{
    public interface ICommand
    {
        string NameKey { get; }
        string TargetNameKey { get; }
        ECommandScope Scope { get; }
        bool IsDisabled(Player mcsBotPlayer);
        void Execute(Player mcsBotPlayer);
        bool IsSubMenu { get; }
        void ExecuteTeam(List<Player> mcsBotPlayers);
    }
}