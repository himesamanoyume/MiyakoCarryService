using System.Collections.Generic;
using System.Linq;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;

namespace MiyakoCarryService.Server
{
    public record ModMetadata : AbstractModMetadata, IModWebMetadata
    {
        private const string CurrentVersion = "1.0.11.0";
#if DEBUG
        public override string Name { get; init; } = "MiyakoCarryServiceServer DebugBuild";
#else
        public override string Name { get; init; } = "MiyakoCarryServiceServer";
#endif
        public override string Author { get; init; } = "Himesamanoyume";
        public override List<string> Contributors { get; init; }
        public override SemanticVersioning.Version Version { get; init; } = new(string.Join('.', CurrentVersion.Split('.', System.StringSplitOptions.None).Take(3)));
        public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
        public override List<string> Incompatibilities { get; init; }
        public override Dictionary<string, SemanticVersioning.Range> ModDependencies { get; init; }
        public override string Url { get; init; } = "https://forge.sp-tarkov.com/mod/2709/miyako-carry-service";
        public override bool? IsBundleMod { get; init; } = false;
        public override string License { get; init; } = "CC BY-NC-SA 4.0";
        public override string ModGuid { get; init; } = "top.himesamanoyume.miyakocarryservice";
        public System.Version ClientVersion { get; init; } = new(CurrentVersion);
    }
}