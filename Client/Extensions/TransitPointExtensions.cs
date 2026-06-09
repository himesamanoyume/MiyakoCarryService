
using System.Runtime.CompilerServices;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TransitPointExtensions
    {
        private static readonly ConditionalWeakTable<TransitPoint, TransitData> _dataDict = new();
        
        extension(TransitPoint transitPoint)
        {
            public TransitData GetData()
            {
                return _dataDict.TryGetValue(transitPoint, out TransitData data) ? data : transitPoint.InitData();
            }

            public TransitData InitData()
            {
                var data = new TransitData(transitPoint);
                _dataDict.Add(transitPoint, data);
                return data;
            }
        }
    }
}