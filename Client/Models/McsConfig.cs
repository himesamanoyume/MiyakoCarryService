
namespace MiyakoCarryService.Client.Models
{
    public class McsPluginConfig
    {
        public McsPluginClientConfig Client;
        public McsPluginServerConfig Server;
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