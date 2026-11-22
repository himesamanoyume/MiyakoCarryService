
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class ExfiltrationPointExtensions
    {

        extension(ExfiltrationPoint exfiltrationPoint)
        {
            public ExfilData GetData()
            {
                return InteropDataCache<ExfilData>.GetOrCreate(exfiltrationPoint.Pointer, () => new ExfilData(exfiltrationPoint));
            }
        }

        public static void Clear()
        {
            InteropDataCache<ExfilData>.Clear();
        }
    }
}