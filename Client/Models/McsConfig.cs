
namespace MiyakoCarryService.Client.Models
{
    public class McsPluginConfig
    {
        public McsPluginClientConfig Client = new();
        public McsPluginServerConfig Server = new();
        public struct McsPluginClientConfig
        {

        }

        public struct McsPluginServerConfig
        {
            public bool CheckUpdate;
            public bool CheckIfdian;
            public bool BalanceRestriction;
        }
    }
}