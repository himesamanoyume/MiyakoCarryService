using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using BepInEx.Bootstrap;

namespace MiyakoCarryService.Fika
{
    [BepInPlugin(McsFikaGUID, McsFikaPluginName, BepInExClientVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    [BepInDependency(BigBrainGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(McsGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(FikaGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MiyakoCarryServiceFikaPlugin : BaseUnityPlugin
    {
        public const string BepInExClientVersion = "0.2.2.0";
        public const string McsGUID = "top.himesamanoyume.miyakocarryservice";
        public const string FikaGUID = "com.fika.core";
        public const string McsFikaGUID = "top.himesamanoyume.miyakocarryservicefika";
        public const string BigBrainGUID = "xyz.drakia.bigbrain";
        public const string McsFikaPluginName = "姫様の夢 MiyakoCarryServiceFika";
        public static MiyakoCarryServiceFikaPlugin Instance;
        public static new readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("MiyakoCarryServiceFika");
        public static bool McsInstalled { get; private set; } = false;
        public static bool FikaInstalled { get; private set; } = false;
        public static bool IsFikaHeadless { get; private set; } = false;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            McsInstalled = !CheckPlugin([McsGUID]);
            FikaInstalled = !CheckPlugin([FikaGUID]);
            IsFikaHeadless = !CheckPlugin(["com.fika.headless"]);
            EnableAllPatches();
        }

        public bool CheckPlugin(List<string> pluginList)
        {
            var pluginInfos = new List<PluginInfo>(Chainloader.PluginInfos.Values);

            foreach (PluginInfo Info in pluginInfos)
            {
                if (pluginList.Contains(Info.Metadata.GUID))
                {
                    return false;
                }
            }
            return true;
        }

        public bool CheckUnsupportedPlugin()
        {
            return CheckPlugin([]);
        }

        private void EnableAllPatches()
        {
            if (FikaInstalled)
            {

            }
        }
    }
}

