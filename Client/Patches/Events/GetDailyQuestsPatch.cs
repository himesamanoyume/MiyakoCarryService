using System.Reflection;
using System.Threading.Tasks;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 修改特定的订单任务为宫子商人的Id
    /// </summary>
    public sealed class GetDailyQuestsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SessionBackendClass), nameof(SessionBackendClass.GetDailyQuests));

        [PatchPostfix]
        public static void Postfix(ref Task<RepeatableQuestsRange[]> __result)
        {
            __result = RewriteOrderTraderIdAsync(__result);
        }

        private static async Task<RepeatableQuestsRange[]> RewriteOrderTraderIdAsync(Task<RepeatableQuestsRange[]> original)
        {
            var result = await original;

            if (result == null)
            {
                return result;
            }

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

            return result;
        }
    }
}