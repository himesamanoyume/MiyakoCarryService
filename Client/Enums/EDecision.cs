using System;

namespace MiyakoCarryService.Client.Enums
{
    [Flags]
    public enum EDecision
    {
        None = 0,
        ShouldEscort = 1 << 0,
        ShouldExfil = 1 << 1,
        ShouldGoToPoint = 1 << 2,
        ShouldHoldPosition = 1 << 3,
        ShouldRegroup = 1 << 4
    }
}