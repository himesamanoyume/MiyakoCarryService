using CommonAssets.Scripts.Cutscenes;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class FinalExitZoneExtensions
    {
        extension(FinallExitZone finalExitZone)
        {
            public FinalExitZoneData GetData()
            {
                if (finalExitZone == null)
                {
                    return null;
                }

                return InteropDataCache<FinalExitZoneData>.GetOrCreate(finalExitZone.Pointer, () => new FinalExitZoneData(finalExitZone));
            }
        }

        public static void Clear()
        {
            InteropDataCache<FinalExitZoneData>.Clear();
        }
    }
}
