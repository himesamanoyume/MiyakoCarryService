
using EFT;

namespace MiyakoCarryService.Client.Extensions
{
    public static class StringExtensions
    {
        extension(string id)
        {
            public string McsLocalized()
            {
                if (id.Localized() == id)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        return LocalizationManager.Instance.LocalizedValue(id, MiyakoCarryServicePlugin.DefaultLang);
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
