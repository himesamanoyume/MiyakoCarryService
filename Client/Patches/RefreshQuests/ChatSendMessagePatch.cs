
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using SPT.Reflection.Patching;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    /// <summary>
    /// 发送任何给宫子商人的消息时，主动请求刷新任务
    /// </summary>
    public sealed class ChatSendMessagePatch : ModulePatch
    {
        private static CancellationTokenSource _questRefreshCts;

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClientBackendSession), nameof(ClientBackendSession.ChatSendMessage));

        [PatchPostfix]
        public static void Postfix(string id, int type, string text, string replyTo, Callback<string> callback)
        {
            if (id != MiyakoCarryServicePlugin.MiyakoTraderId)
            {
                return;
            }

            _questRefreshCts?.Cancel();
            _questRefreshCts?.Dispose();
            _questRefreshCts = new CancellationTokenSource();
            TasksExtensions.HandleExceptions(DelayedRefreshAsync(_questRefreshCts.Token));
        }

        private static async Task DelayedRefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(true);
                EventMgr.Notify(new UpdateDailyQuestsEvent());
            }
            catch (OperationCanceledException)
            {

            }
        }
    }
}