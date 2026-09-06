
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class ExfiltrationPointExtensions
    {
        private static readonly AttachedTable<ExfiltrationPoint, ExfilData> _datas = new();

        extension(ExfiltrationPoint exfiltrationPoint)
        {
            public ExfilData GetData()
            {
                return _datas.GetOrCreate(exfiltrationPoint, key => new ExfilData(key));
            }
        }
    }
}