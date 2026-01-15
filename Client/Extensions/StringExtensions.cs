
namespace MiyakoCarryService.Client.Extensions
{
    internal static class StringExtensions
    {
        extension(string id)
        {
            public string McsLocalized()
            {
                if (id.Localized() == id)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        return LocaleManagerClass.LocaleManagerClass.method_7(id, MiyakoCarryServicePlugin.DefaultLang);
                    }
                    return id;
                }
                else
                {
                    return id.Localized();
                }
            }
        }
    }
}
