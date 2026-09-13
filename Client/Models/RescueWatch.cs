using UnityEngine;

namespace MiyakoCarryService.Client.Models
{
    public class RescueWatch
    {
        public Vector3 LastGoodPos;
        public bool HasLastGood;
        public Vector3 Anchor;
        public float AnchorTime;
        public float NextProbeTime;
    }
}
