
using UnityEngine;

namespace MiyakoCarryService.Client.Datas
{
    internal abstract class BaseData
    {
        protected GameLoop _gameloop;

        public BaseData()
        {
            _gameloop = GameLoop.Instance;
        }
    }
}