

namespace MiyakoCarryService.Client.Utils
{
    internal static class Tools
    {
        public static bool CheckGameWorld()
        {
            return GameLoop.Instance.IsVaildGameWorld;
        }

        public static bool IsPlayerInventory(string id)
        {
            return id == CommonId.DefaultInventory;
        }
    }
}