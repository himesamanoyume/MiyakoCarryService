
using EFT.SynchronizableObjects;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TripwireSynchronizableObjectExtensions
    {

        extension(TripwireSynchronizableObject tripwireSynchronizableObject)
        {
            public TripwireData GetData()
            {
                return InteropDataCache<TripwireData>.GetOrCreate(tripwireSynchronizableObject.Pointer, () => new TripwireData(tripwireSynchronizableObject));
            }
        }

        public static void Clear()
        {
            InteropDataCache<TripwireData>.Clear();
        }
    }
}