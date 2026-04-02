
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class BaseData
    {
        protected GameLoop _gameloop;

        public BaseData()
        {
            _gameloop = GameLoop.Instance;
        }
    }
}