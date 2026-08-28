

namespace MiyakoCarryService.Client.Extensions
{
    public static class BotBewareGrenadeExtensions
    {
        extension(BotBewareGrenade botBewareGrenade)
        {
            public bool McsShallRunAway()
            {
                if (botBewareGrenade.GrenadeDangerPoint == null)
                {
                    return false;
                }

                if (botBewareGrenade.IsIgnoreByPeriod())
                {
                    return false;
                }

                if (botBewareGrenade.GrenadeDangerPoint.ShallDestroy())
                {
                    botBewareGrenade.GrenadeDangerPoint = null;
                    return false;
                }

                if (!botBewareGrenade.GrenadeDangerPoint.McsShallRunAway())
                {
                    return false;
                }

                if (botBewareGrenade.IgnoreGrenade(botBewareGrenade.GrenadeDangerPoint.Grenade))
                {
                    return false;
                }

                return botBewareGrenade.GrenadeDangerPoint.IsActive();
            }
        }
    }

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