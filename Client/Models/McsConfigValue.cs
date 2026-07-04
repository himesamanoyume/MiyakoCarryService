
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Models
{
    public sealed class McsConfigValue
    {
        public EMcsConfigValueType Type;
        public bool BoolValue;
        public long IntValue;
        public float FloatValue;
        public string StringValue;
    }
}