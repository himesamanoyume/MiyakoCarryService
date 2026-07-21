using System.Collections.Generic;
using System.Linq;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;

namespace MiyakoCarryService.Server
{
    public record ModMetadata : IModMetadata, IModBlazorMetadata
    {
        private const string CurrentVersion = "1.1.0.0";
#if DEBUG
        public string Name { get; init; } = "MiyakoCarryServiceServer DebugBuild";
#else
        public string Name { get; init; } = "MiyakoCarryServiceServer";
#endif
        public string Author { get; init; } = "Himesamanoyume";
        public List<string> Contributors { get; init; }
        public SemanticVersioning.Version Version { get; init; } = new(string.Join('.', CurrentVersion.Split('.', System.StringSplitOptions.None).Take(3)));
        public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
        public List<string> Incompatibilities { get; init; }
        public Dictionary<string, SemanticVersioning.Range> ModDependencies { get; init; }
        public string Url { get; init; } = "https://forge.sp-tarkov.com/mod/2709/miyako-carry-service";
        public string License { get; init; } = "CC BY-NC-SA 4.0";
        public string ModGuid { get; init; } = "top.himesamanoyume.miyakocarryservice";
        public System.Version ClientVersion { get; init; } = new(CurrentVersion);
        public bool HasPrepatcher { get; init; }
        public string WWWRootUrl { get; init; }
        public string HomePage { get; init; }
        public string HomePageDescription { get; init; }
    }
}