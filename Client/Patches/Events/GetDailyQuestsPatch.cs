using System.Reflection;
using System.Threading.Tasks;
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
        public static async void Postfix(Task<DailyQuestClass[]> __result)
        {
            await __result;
            foreach (var dailyQuestClass in __result.Result)
            {
                if (dailyQuestClass.Name == "Order")
                {
                    foreach (var quest in dailyQuestClass.Quests)
                    {
                        quest.TraderId = "6952ced4bcc1dd1e3c80dfcb";
                    }
                    break;
                }
            }
        }
    }
}