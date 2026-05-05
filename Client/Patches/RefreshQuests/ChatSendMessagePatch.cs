
using System.Reflection;
using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    /// <summary>
    /// 当可能发送下单相关的消息时进行主动请求刷新任务
    /// </summary>
    public sealed class ChatSendMessagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ProfileEndpointFactoryAbstractClass), nameof(ProfileEndpointFactoryAbstractClass.ChatSendMessage));

        [PatchPostfix]
        public static void Postfix(string id, int type, string text, string replyTo, Callback<string> callback)
        {
            if (text.Contains("mcs order"))
            {
                _ = TraderScreensGroupShowPatch.UpdateDailyQuests();
            }
        }
    }
}