

using Comfort.Common;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class LabyrinthDataMgr<T> : DataMgr<T> where T : MonoBehaviour
    {
        protected bool _shouldInit => Singleton<GameWorld>.Instance.LocationId == "Labyrinth";
    }
}