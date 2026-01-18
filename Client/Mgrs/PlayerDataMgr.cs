

using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class PlayerDataMgr : DataMgr<PlayerDataMgr>
    {
        public List<McsPlayerData> GetMcsPlayerDatas()
        {
            var result = new List<McsPlayerData>();
            foreach (BaseData baseData in _datas)
            {
                if (baseData is McsPlayerData mcsPlayerData)
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