
namespace MiyakoCarryService.Client.Extensions
{
    internal static class StringExtensions
    {
        public static string MiyakoCarryServiceLocalized(this string id)
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
