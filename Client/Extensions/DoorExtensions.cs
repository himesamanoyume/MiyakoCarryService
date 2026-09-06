using EFT.Interactive;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class DoorExtensions
    {
        private static readonly AttachedTable<Door, DoorData> _datas = new();

        extension(Door door)
        {
            public DoorData GetData()
            {
                return _datas.GetOrCreate(door, key => new DoorData(key));
            }
        }
    }
}