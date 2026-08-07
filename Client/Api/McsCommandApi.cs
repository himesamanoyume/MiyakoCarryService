

using System;
using System.Collections.Generic;
using EFT;
using EFT.UI;
using MiyakoCarryService.Client.Mgrs;
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
        public static void RegisterCommandMenu(string menuKey, Action<McsCommandMenu, Player[]> menu)
        {
            CommandUtils.RegisterCommandMenu(menuKey, menu);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RegisterCommandHandler(string commandTypeName, McsCommandHandler handler)
        {
            CommandUtils.RegisterCommandHandler(commandTypeName, handler);
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

        /// <summary>
        /// 枚举当前战局"代理/护送"类指令菜单选项（含本地化名称与距离提示），供语音管线注入 LLM 提示词。
        /// 战局外或失败时返回空列表。
        /// </summary>
        public static List<VoiceMenuOption> GetVoiceMenuOptions()
        {
            try
            {
                var mgr = MgrAccessor.Get<CommandMgr>();
                if (mgr == null)
                {
                    return new List<VoiceMenuOption>();
                }
                return mgr.GetVoiceProxyEscortOptions(CommandUtils.GetAliveMembers());
            }
            catch
            {
                return new List<VoiceMenuOption>();
            }
        }
        
        public static void Execute(McsCommandContext ctx, bool shouldCheckData)
        {
            CommandUtils.Execute(ctx, shouldCheckData);
        }
    }
}