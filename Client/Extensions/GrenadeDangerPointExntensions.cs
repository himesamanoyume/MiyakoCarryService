
namespace MiyakoCarryService.Client.Extensions
{
    public static class GrenadeDangerPointExntensions
    {
        extension(GrenadeDangerPoint grenadeDangerPoint)
        {
            public bool McsShallRunAway()
            {
                if (grenadeDangerPoint.IsActive() && (grenadeDangerPoint._owner.Transform.position - grenadeDangerPoint.DangerPoint).sqrMagnitude <= 64f)
                {
                    return true;
                }

                return false;
            }
        }
    }
}