using UnityEngine;

namespace MiyakoCarryService.Client.Models
{
    public struct VaultBlacklistEntry
    {
        public Vector3 Pos;
        public float Until;
        public int Fails;
    }
}
