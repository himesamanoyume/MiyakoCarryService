using System.Collections.Generic;

namespace MiyakoCarryService.Client.Utils
{
    public static class LocalLocales
    {
        public static Dictionary<string, Dictionary<string, string>> LoadingLocales = new()
        {
            {
                "ch", new()
                {
                    { 
                        "Mcs/LoadingLocales",
                        "当前正在尝试加载本地化文本, 如果您发现长期处于当前状态, 请确保您已正确安装服务端Mod MiyakoCarryServiceServer, 否则无法获取到本地化文本"
                    }
                }
            },
            {
                "en", new()
                {
                    { 
                        "Mcs/LoadingLocales",
                        "Attempting to load localization texts. If this persists, ensure the server mod MiyakoCarryServiceServer is installed; otherwise localization cannot be retrieved"
                    }
                }
            }
        };
    }
}
