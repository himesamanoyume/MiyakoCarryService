using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 更新商人供货数据
    /// </summary>
    internal sealed class TraderClassConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.FirstConstructor(typeof(TraderClass), x => true);

        public static void SetSupplyData(TraderClass trader, SupplyData supplyData)
        {
            trader.SupplyData_0 = supplyData;
        }

        private static async void UpdateSupplyData(TraderClass trader)
        {
            Result<SupplyData> result = await GameLoop.Instance.Session.GetSupplyData(trader.Id);
            if (result.Succeed)
            {
                SetSupplyData(trader, result.Value);
            }
        }

        [PatchPostfix]
        public static void Postfix(ref TraderClass __instance)
        {
            UpdateSupplyData(__instance);
        }
    }
}