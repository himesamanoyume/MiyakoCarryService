
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TransitPointExtensions
    {
        private static readonly AttachedTable<TransitPoint, TransitData> _datas = new();

        extension(TransitPoint transitPoint)
        {
            public TransitData GetData()
            {
                return _datas.GetOrCreate(transitPoint, key => new TransitData(key));
            }
        }
    }
}