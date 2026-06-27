using System.Runtime.CompilerServices;
using EFT.Interactive;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Extensions
{
    public static class DoorExtensions
    {
        private static readonly ConditionalWeakTable<Door, DoorData> _dataDict = new();
        
        extension(Door door)
        {
            public DoorData GetData()
            {
                return _dataDict.TryGetValue(door, out DoorData data) ? data : door.InitData();
            }

            public DoorData InitData()
            {
                var data = new DoorData(door);
                _dataDict.Add(door, data);
                return data;
            }
        }
    }
}