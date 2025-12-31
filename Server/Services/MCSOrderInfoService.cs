using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiyakoCarryService.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class MCSOrderInfoService(
        MCSConfigService mcsConfigService,
        JsonUtil jsonUtil,
        FileUtil fileUtil
    )
    {
        private readonly string _orderFolderDir = System.IO.Path.Join(mcsConfigService.GetModPath(), "Assets", "database", "orders");
        private readonly ConcurrentDictionary<MongoId, MCSOrderInfo> _orderInfos = new();

        public void AddOrderInfo(MCSOrderInfo orderInfo)
        {
            _orderInfos.TryAdd(orderInfo.QuestId, orderInfo);
        }

        public void AddOrderInfos(List<MCSOrderInfo> orderInfos)
        {
            foreach (var orderInfo in orderInfos)
            {
                AddOrderInfo(orderInfo);
            }
        }

        public void RemoveOrderInfo(MCSOrderInfo orderInfo)
        {
            _orderInfos.TryRemove(orderInfo.QuestId, out _);
        }

        public void SaveOrderInfo()
        {
            var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
            var orderInfos = _orderInfos.Values.ToList();
            var jsonMCSOrderInfos = jsonUtil.Serialize(orderInfos, false);
            fileUtil.WriteFile(orderPath, jsonMCSOrderInfos);
        }

        public List<MCSOrderInfo> GetOrderInfos(MongoId sessionId)
        {
            List<MCSOrderInfo> targetMCSOrderInfos = new();

            foreach (var mcsOrderInfo in _orderInfos.Values.ToList())
            {
                if (mcsOrderInfo.SessionId == sessionId)
                {
                    targetMCSOrderInfos.Add(mcsOrderInfo);
                }
            }

            return targetMCSOrderInfos;
        }

        public List<MCSOrderInfo> GetAllOrderInfos()
        {
            return _orderInfos.Values.ToList();
        }

        public async Task LoadAllOrderInfos()
        {
            var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
            var orderInfos = await jsonUtil.DeserializeFromFileAsync<List<MCSOrderInfo>>(orderPath) ?? new List<MCSOrderInfo>();
            foreach (var orderInfo in orderInfos)
            {
                _orderInfos[orderInfo.QuestId] = orderInfo;
            }
        }

        public async Task OnPostLoadAsync()
        {
            await LoadAllOrderInfos();
        }
    }
}