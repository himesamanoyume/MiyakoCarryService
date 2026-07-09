using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Trading;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 更新商人供货数据
    /// </summary>
    public sealed class TraderClassConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.FirstConstructor(typeof(Trader), x => true);

        public static void SetSupplyData(Trader trader, SupplyData supplyData)
        {
            trader.supplyData_0 = supplyData;
        }

        private static async void UpdateSupplyData(Trader trader)
        {
            Result<SupplyData> result = await GameLoop.Instance.Session.GetSupplyData(trader.Id);
            if (result.Succeed)
            {
                SetSupplyData(trader, result.Value);
            }
        }

        [PatchPostfix]
        public static void Postfix(ref Trader __instance)
        {
            UpdateSupplyData(__instance);
        }
    }
}