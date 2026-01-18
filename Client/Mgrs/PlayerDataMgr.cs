

using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class PlayerDataMgr : DataMgr<PlayerDataMgr>
    {
        public List<McsPlayerData> GetMcsPlayerAllDatas()
        {
            var result = new List<McsPlayerData>();
            foreach (BaseData item in _allDatas)
            {
                if (item is McsPlayerData mcsPlayerData)
                {
                    result.Add(mcsPlayerData);
                }
            }
            return result;
        }
        
        public sealed override void Start()
        {
            base.Start();
        }

        protected override void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}