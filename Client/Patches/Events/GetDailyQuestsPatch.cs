using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.Quests;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using SPTushonka.Reflection.Il2Cpp;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 修改特定的订单任务为宫子商人的Id
    /// </summary>
    public sealed class GetDailyQuestsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EftClientBackendSession), "EFT.IQuestSession.GetDailyQuests");

        [PatchPostfix]
        public static void Postfix(ref Il2CppSystem.Threading.Tasks.Task<Il2CppReferenceArray<RepeatableQuestsRange>> __result)
        {
            __result = RewriteOrderTraderIdAsync(__result);
        }

        private static Il2CppSystem.Threading.Tasks.Task<Il2CppReferenceArray<RepeatableQuestsRange>> RewriteOrderTraderIdAsync(Il2CppSystem.Threading.Tasks.Task<Il2CppReferenceArray<RepeatableQuestsRange>> original)
        {
            var completion = new Il2CppSystem.Threading.Tasks.TaskCompletionSource<Il2CppReferenceArray<RepeatableQuestsRange>>();

            async Task Load()
            {
                try
                {
                    // 拉回主线程后再改 trader/quest 数据（原续体在线程池，改游戏对象会 AV）
                    await original.AsManaged();
                    var result = original.Result;

                    if (result != null)
                    {
                        foreach (var dailyQuestClass in result)
                        {
                            if (dailyQuestClass.Name == "Order")
                            {
                                foreach (var quest in dailyQuestClass.Quests)
                                {
                                    quest.TraderId = MiyakoCarryServicePlugin.MiyakoTraderId;
                                }

                                break;
                            }
                        }
                    }

                    completion.TrySetResult(result);
                }
                catch (System.Exception ex)
                {
                    McsLogger.LogError(ex);
                    completion.TrySetException(new Il2CppSystem.Exception(ex.Message));
                }
            }

            _ = Load();
            return completion.Task;
        }
    }
}