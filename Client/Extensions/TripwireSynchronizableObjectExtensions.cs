
using EFT.SynchronizableObjects;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TripwireSynchronizableObjectExtensions
    {
        private static readonly AttachedTable<TripwireSynchronizableObject, TripwireData> _datas = new();

        extension(TripwireSynchronizableObject tripwireSynchronizableObject)
        {
            public TripwireData GetData()
            {
                return _datas.GetOrCreate(tripwireSynchronizableObject, key => new TripwireData(key));
            }
        }
    }
}