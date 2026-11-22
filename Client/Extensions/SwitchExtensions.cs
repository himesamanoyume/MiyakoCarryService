
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class SwitchExtensions
    {

        extension(Switch @switch)
        {
            public SwitchData GetData()
            {
                return InteropDataCache<SwitchData>.GetOrCreate(@switch.Pointer, () => new SwitchData(@switch));
            }
        }

        public static void Clear()
        {
            InteropDataCache<SwitchData>.Clear();
        }
    }
}