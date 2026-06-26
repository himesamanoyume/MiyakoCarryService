
using System.Runtime.CompilerServices;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Extensions
{
    public static class SwitchExtensions
    {
        private static readonly ConditionalWeakTable<Switch, SwitchData> _dataDict = new();
        
        extension(Switch @switch)
        {
            public SwitchData GetData()
            {
                return _dataDict.TryGetValue(@switch, out SwitchData data) ? data : @switch.InitData();
            }

            public SwitchData InitData()
            {
                var data = new SwitchData(@switch);
                _dataDict.Add(@switch, data);
                return data;
            }
        }
    }
}