using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
        private readonly ConcurrentDictionary<MongoId, SemaphoreSlim> _saveLocks = new();

        public async Task AddOrderInfo(MongoId sessionId, MCSOrderInfo orderInfo)
        {
            var saveLock = _saveLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
            await saveLock.WaitAsync();
            try
            {
                var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
                var mcsOrderInfos = await jsonUtil.DeserializeFromFileAsync<List<MCSOrderInfo>>(orderPath) ?? new List<MCSOrderInfo>();
                mcsOrderInfos.Add(orderInfo);
                var jsonMCSOrderInfos = jsonUtil.Serialize(mcsOrderInfos, false);
                await fileUtil.WriteFileAsync(orderPath, jsonMCSOrderInfos);
            }
            finally
            {
                saveLock.Release();
            }
        }

        public async Task SaveOrderInfo(MongoId sessionId, List<MCSOrderInfo> orderInfos)
        {
            var saveLock = _saveLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
            await saveLock.WaitAsync();
            try
            {
                var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
                var jsonMCSOrderInfos = jsonUtil.Serialize(orderInfos, false);
                await fileUtil.WriteFileAsync(orderPath, jsonMCSOrderInfos);
            }
            finally
            {
                saveLock.Release();
            }
        }

        public async Task<List<MCSOrderInfo>> GetOrderInfos(MongoId sessionId)
        {
            List<MCSOrderInfo> targetMCSOrderInfos = new();
            var saveLock = _saveLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
            await saveLock.WaitAsync();
            try
            {
                var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
                var mcsOrderInfos = await jsonUtil.DeserializeFromFileAsync<List<MCSOrderInfo>>(orderPath) ?? new List<MCSOrderInfo>();
                foreach (var mcsOrderInfo in mcsOrderInfos)
                {
                    if (mcsOrderInfo.SessionId == sessionId)
                    {
                        targetMCSOrderInfos.Add(mcsOrderInfo);
                    }
                }
            }
            finally
            {
                saveLock.Release();
            }
            return targetMCSOrderInfos;
        }

        public async Task<List<MCSOrderInfo>> GetAllOrderInfos(MongoId sessionId)
        {
            var saveLock = _saveLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
            await saveLock.WaitAsync();
            try
            {
                var orderPath = System.IO.Path.Combine(_orderFolderDir, "orderinfo.json");
                var mcsOrderInfos = await jsonUtil.DeserializeFromFileAsync<List<MCSOrderInfo>>(orderPath) ?? new List<MCSOrderInfo>();
                return mcsOrderInfos;
            }
            finally
            {
                saveLock.Release();
            }
        }
    }
}