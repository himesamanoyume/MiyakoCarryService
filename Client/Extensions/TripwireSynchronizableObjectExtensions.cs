
using System.Runtime.CompilerServices;
using EFT.SynchronizableObjects;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TripwireSynchronizableObjectExtensions
    {
        private static readonly ConditionalWeakTable<TripwireSynchronizableObject, TripwireData> _dataDict = new();
        
        extension(TripwireSynchronizableObject tripwireSynchronizableObject)
        {
            public TripwireData GetData()
            {
                return _dataDict.TryGetValue(tripwireSynchronizableObject, out TripwireData data) ? data : tripwireSynchronizableObject.InitData();
            }

            public TripwireData InitData()
            {
                var data = new TripwireData(tripwireSynchronizableObject);
                _dataDict.Add(tripwireSynchronizableObject, data);
                return data;
            }
        }
    }
}