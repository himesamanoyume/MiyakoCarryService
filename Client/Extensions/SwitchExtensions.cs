
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class SwitchExtensions
    {
        private static readonly AttachedTable<Switch, SwitchData> _datas = new();

        extension(Switch @switch)
        {
            public SwitchData GetData()
            {
                return _datas.GetOrCreate(@switch, key => new SwitchData(key));
            }
        }
    }
}