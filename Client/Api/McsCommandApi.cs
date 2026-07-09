

using System;
using EFT;
using EFT.UI;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Api
{
    public static class McsCommandApi
    {
        /// <summary>
        /// 
        /// </summary>
        public static void OnCurrentActionChanged()
        {
            CommandUtils.OnCurrentActionChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void PreBuildCommandMenu(out AvailableInteractionState actionsReturnClass)
        {
            CommandUtils.PreBuildCommandMenu(out actionsReturnClass);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void PostBuildCommandMenu(AvailableInteractionState actionsReturnClass)
        {
            CommandUtils.PostBuildCommandMenu(actionsReturnClass);
        }

        /// <summary>
        /// 
        /// </summary>
        public static InteractionAction MakeCommand(string name, string targetName, bool disabled, Action action)
        {
            return CommandUtils.MakeCommand(name, targetName, disabled, action);
        }

        /// <summary>
        /// 
        /// </summary>
        public static GamePlayerOwner GetMyGamePlayerOwner()
        {
            return CommandUtils.GamePlayerOwner;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Register(string menuKey, Action<McsCommandMenu, Player[]> menu)
        {
            CommandUtils.Register(menuKey, menu);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ClearGamePlayerOwner()
        {
            CommandUtils.ClearGamePlayerOwner();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ClearMenuStack()
        {
            CommandUtils.ClearMenuStack();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Apply(string menuKey, McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            CommandUtils.Apply(menuKey, menu, mcsBotPlayers);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CloseCommandMenuAction()
        {
            CommandUtils.CloseCommandMenuAction();
        }

        public static Player[] GetAliveMembers()
        {
            return CommandUtils.GetAliveMembers();
        }
    }
}