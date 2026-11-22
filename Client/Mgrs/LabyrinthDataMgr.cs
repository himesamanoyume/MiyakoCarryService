

using Comfort.Common;
using EFT;
using System;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class LabyrinthDataMgr : DataMgr
    {
        protected LabyrinthDataMgr(IntPtr pointer) : base(pointer)
        {
        }

        protected bool _shouldInit => Singleton<GameWorld>.Instance.LocationId == "Labyrinth";
    }
}