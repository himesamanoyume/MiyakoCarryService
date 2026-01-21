

using System.Collections.Generic;
using MiyakoCarryService.Client.Datas;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class PlayerDataMgr : DataMgr<PlayerDataMgr>
    {
        public List<McsBotPlayerData> GetMcsBotPlayerDatas()
        {
            var result = new List<McsBotPlayerData>();
            foreach (BaseData baseData in _datas)
            {
                if (baseData is McsBotPlayerData mcsBotPlayerData)
                {
                    result.Add(mcsBotPlayerData);
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