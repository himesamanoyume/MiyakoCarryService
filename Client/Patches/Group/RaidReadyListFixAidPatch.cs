
using System.Reflection;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Group
{
    /// <summary>
    /// 修复SPT机器人Aid重复为1234566的问题（服务端最新生成SPT机器人消息时虽然会修改Aid，但是对于已经生成过消息的存档则无法覆盖，需要在客户端再进行一次处理）
    /// </summary>
    public sealed class RaidReadyListFixAidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(RaidReadyList.Class3311), nameof(RaidReadyList.Class3311.method_0));

        [PatchPrefix]
        public static bool Prefix(RaidReadyList.Class3311 __instance, ref GroupPlayerViewModelClass player, RaidReadyPlayerPanel playerPanel)
        {
            if (player.Info.Nickname != "SPT")
            {
                return true;
            }

            if (player.AccountId != "1234566")
            {
                return true;
            }

            player.AccountId = "1234568";
            foreach (var friend in __instance.friends)
            {
                if (friend.AccountId == "1234566")
                {
                    friend.AccountId = "1234568";
                    break;
                }
            }
            return true;
        }
    }
}