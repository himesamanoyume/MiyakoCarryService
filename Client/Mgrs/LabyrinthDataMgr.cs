

using Comfort.Common;
using EFT;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class LabyrinthDataMgr : DataMgr<LabyrinthDataMgr>
    {
        protected bool _shouldInit => Singleton<GameWorld>.Instance.LocationId == "labyrinth";
    }
}