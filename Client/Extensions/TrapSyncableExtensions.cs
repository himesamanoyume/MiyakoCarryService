
using CommonAssets.Scripts.Game.LabyrinthEvent;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Extensions
{
    public static class TrapSyncableExtensions
    {
        private static readonly AttachedTable<TrapSyncable, BarbedWireData> _datas = new();

        extension(TrapSyncable trapSyncable)
        {
            public BarbedWireData GetData()
            {
                return _datas.GetOrCreate(trapSyncable, key => new BarbedWireData(key));
            }
        }
    }
}