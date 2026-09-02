

using System.Collections.Generic;
using UnityEngine;

namespace MiyakoCarryService.Client.Models
{
    public class McsCommandRequest
    {
        public string CommandType;
        public string TargetId;
        public Vector3? Position;
        public BodyPartType AimingBodyPartType;
        public bool ShouldCheckExclude;
        public Dictionary<string, McsValue> Extensions = new();
    }
}
