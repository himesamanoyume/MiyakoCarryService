
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TransitPointExtensions
    {

        extension(TransitPoint transitPoint)
        {
            public TransitData GetData()
            {
                return InteropDataCache<TransitData>.GetOrCreate(transitPoint.Pointer, () => new TransitData(transitPoint));
            }
        }

        public static void Clear()
        {
            InteropDataCache<TransitData>.Clear();
        }
    }
}