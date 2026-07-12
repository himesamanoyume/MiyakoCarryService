

using BepInEx;
using EFT;
using MiyakoCarryService.Client.Api;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Models;

namespace MiyakoCarryService.Client.Addon
{
    [BepInPlugin(McsAIGUID, McsAIName, MiyakoCarryServicePlugin.BepInExClientVersion)]
    [BepInProcess(MiyakoCarryServicePlugin.EFTapp)]
    [BepInDependency(MiyakoCarryServicePlugin.BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MiyakoCarryServicePlugin.McsGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MiyakoCarryServicePlugin.FikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MiyakoCarryServiceClientAddonPlugin : BaseUnityPlugin
    {
        public const string McsAIGUID = "top.himesamanoyume.miyakocarryservice.ai";
#if DEBUG
        public const string McsAIName = "姫様の夢 MiyakoCarryServiceAI DebugBuild";
#else
        public const string McsAIName = "姫様の夢 MiyakoCarryServiceAI";
#endif

        void Start()
        {
            McsCommandApi.RegisterCommandMenu(EMenuId.Team.ToString(), BuildTestSubMenu);
            McsCommandApi.RegisterCommandMenu(EMenuId.Member.ToString(), BuildMemberSubMenu);
            McsCommandApi.RegisterCommandHandler("AddonTestType", TestCommandAction);
        }

        private void BuildTestSubMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterSubMenu("测试子菜单1", "测试子菜单1说明", (m) => TestSubMenu(m, mcsBotPlayers));
            menu.RegisterCommand("测试指令1", "测试指令1说明", "AddonTestType", mcsBotPlayers);
        }

        private void BuildMemberSubMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterSubMenu("测试子菜单2", "测试子菜单2说明", (m) => MemberSubMenu(m, mcsBotPlayers));
            menu.RegisterCommand("测试指令3", "测试指令3说明", "AddonTestType", mcsBotPlayers);
        }

        private void TestSubMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterCommand("测试指令2", "测试指令2说明", "AddonTestType", mcsBotPlayers);
        }

        private void MemberSubMenu(McsCommandMenu menu, Player[] mcsBotPlayers)
        {
            menu.RegisterCommand("测试指令4", "测试指令4说明", "AddonTestType", mcsBotPlayers);
        }

        private void TestCommandAction(McsCommandContext ctx)
        {
            McsCommandApi.CloseCommandMenuAction();
        }
    }
}
