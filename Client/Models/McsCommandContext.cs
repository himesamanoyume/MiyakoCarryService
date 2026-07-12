using System.Collections.Generic;
using EFT;
using UnityEngine;

namespace MiyakoCarryService.Client.Models
{
    public delegate void McsCommandHandler(McsCommandContext ctx);
    
    public sealed class McsCommandContext
    {
        public Player McsLeadPlayer;
        public Player McsBotPlayer;

        public string CommandType;
        public Vector3? Position;
        public BodyPartType AimingBodyPartType;
        public string TargetId;
        public Dictionary<string, McsValue> Extensions = new();
    }
}