
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ChatShared;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.RefreshQuests
{
    /// <summary>
    /// 收到宫子商人的下单/罚单成功模板消息时，主动请求刷新任务
    /// </summary>
    public sealed class MessageReceivedHandlerPatch : ModulePatch
    {
        private static CancellationTokenSource _questRefreshCts;

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SocialNetwork), nameof(SocialNetwork.MessageReceivedHandler));

        [PatchPostfix]
        public static void Postfix(DialogueChatMessage message, string dialogueId)
        {
            if (dialogueId != MiyakoCarryServicePlugin.MiyakoTraderId)
            {
                return;
            }

            var templateId = message?.templateId;
            if (templateId != Locales.MIYAKOTRADERORDERNEWQUEST && templateId != Locales.MIYAKOTRADERTICKETNEWQUEST)
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
