
using UnityEngine;

namespace MiyakoCarryService.Client.Models
{
    public class McsMsg
    {
        public EPhraseTrigger PhraseTrigger = EPhraseTrigger.None;
        public Vector3? Position = null;
        public string Key = null;
        public string Key2 = null;
    }
}