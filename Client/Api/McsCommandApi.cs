

using System;
using EFT;
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
        public static void PreBuildCommandMenu(out ActionsReturnClass actionsReturnClass)
        {
            CommandUtils.PreBuildCommandMenu(out actionsReturnClass);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void PostBuildCommandMenu(ActionsReturnClass actionsReturnClass)
        {
            CommandUtils.PostBuildCommandMenu(actionsReturnClass);
        }

        /// <summary>
        /// 
        /// </summary>
        public static ActionsTypesClass MakeCommand(string name, string targetName, bool disabled, Action action)
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
        public static void RegisterCommandMenu(string menuKey, Action<McsCommandMenu, Player[]> menu)
        {
            CommandUtils.RegisterCommandMenu(menuKey, menu);
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
        
        public static void Execute(McsCommandContext ctx)
        {
            CommandUtils.Execute(ctx);
        }
    }
}