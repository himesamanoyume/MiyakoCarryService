
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Models
{
    public sealed class McsValue
    {
        public EMcsValueType Type;
        public bool BoolValue;
        public long IntValue;
        public double DoubleValue;
        public float FloatValue;
        public string StringValue;
    }
}