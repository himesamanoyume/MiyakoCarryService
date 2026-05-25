
namespace MiyakoCarryService.Client.Extensions
{
    public static class BotReloadExtensions
    {
        extension(BotReload botReload)
        {
            public void McsTryReload()
            {
                var canReload = true;
                if (botReload.Reloading)
                {
                    botReload.CheckReloadLongTime();
                    canReload = false;
                }

                if (canReload)
                {
                    if (botReload.CanReload(true, out var magazineItemClass, out var list))
                    {
                        botReload.Reload();
                    }
                }
            }
        }
    }
}
