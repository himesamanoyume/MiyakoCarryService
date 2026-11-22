using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Trading;
using HarmonyLib;
using MiyakoCarryService.Client.Utils;
using SPTushonka.Reflection.Patching;
using SPTushonka.Reflection.Il2Cpp;

namespace MiyakoCarryService.Client.Patches.Events
{
    /// <summary>
    /// 更新商人供货数据
    /// </summary>
    public sealed class TraderClassConstructorPatch : ModulePatch
    {
        private static readonly HashSet<string> _inFlightTraderIds = new();

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EftClientBackendSession), nameof(EftClientBackendSession.UpdateTraders));

        public static void SetSupplyData(Trader trader, SupplyData supplyData)
        {
            trader._supplyData = supplyData;
        }

        /// <summary>
        /// 主动登记一次供货数据拉取。5.0 下物品估价依赖 <c>Trader._supplyData</c>，
        /// 未就绪时必须先跳过估价并在此登记，拿到后 <see cref="ItemBestPriceDict"/> 会被清空并重算。
        /// </summary>
        public static void RequestSupplyData(Trader trader)
        {
            UpdateSupplyData(trader);
        }

        private static async void UpdateSupplyData(Trader trader)
        {
            if (trader == null)
            {
                return;
            }

            var traderId = trader.Id;
            if (!_inFlightTraderIds.Add(traderId))
            {
                return;
            }

            try
            {
                var backendSession = GameLoop.Instance.Session.TryCast<ClientBackendSession>();
                if (backendSession == null)
                {
                    return;
                }

                Result<SupplyData> result = await backendSession.GetSupplyData(traderId).AsManaged();
                if (result.Succeed)
                {
                    SetSupplyData(trader, result.Value);
                    GameLoop.Instance.ItemBestPriceDict.Clear();
                }
            }
            catch (System.Exception e)
            {
                McsLogger.LogError($"获取商人供货数据失败 [{traderId}]: {e}");
            }
            finally
            {
                _inFlightTraderIds.Remove(traderId);
            }
        }

        [PatchPostfix]
        public static void Postfix(EftClientBackendSession __instance)
        {
            var traders = __instance?._traders;
            if (traders == null)
            {
                return;
            }

            for (int i = 0; i < traders.Count; i++)
            {
                var trader = traders[i];
                if (trader != null && trader._supplyData == null)
                {
                    UpdateSupplyData(trader);
                }
            }
        }
    }
}