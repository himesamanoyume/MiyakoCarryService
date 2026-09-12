
using MiyakoCarryService.Client.Enums;
using UnityEngine;

namespace MiyakoCarryService.Client.Models
{
    public class NavGapInfo
    {
        public ENavGapType Type;
        public Vector3 NearPoint;
        public Vector3 FarPoint;
        public Vector3[] Way;
    }
}