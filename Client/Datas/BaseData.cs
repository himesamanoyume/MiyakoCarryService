
using System;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class BaseData : IDisposable
    {
        protected GameLoop _gameloop;

        public BaseData()
        {
            _gameloop = GameLoop.Instance;
        }

        public virtual void Dispose()
        {
            _gameloop = null;
        }
    }
}