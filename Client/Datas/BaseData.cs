
using System;

namespace MiyakoCarryService.Client.Datas
{
    public abstract class BaseData : IDisposable
    {
        public GameLoop Gameloop;

        public BaseData()
        {
            Gameloop = GameLoop.Instance;
        }

        public virtual void Dispose()
        {
            Gameloop = null;
        }
    }
}