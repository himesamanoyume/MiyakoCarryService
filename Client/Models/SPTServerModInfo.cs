using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class SPTServerModInfo
    {
        [DataMember(Name = "ModGuid")]
        public string ModGuid { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Author")]
        public string Author { get; set; }

        [DataMember(Name = "Contributors")]
        public List<string> Contributors { get; set; }

        [DataMember(Name = "Version")]
        public string Version { get; set; }

        [DataMember(Name = "SptVersion")]
        public string SptVersion { get; set; }

        [DataMember(Name = "HasPrepatcher")]
        public bool HasPrepatcher { get; set; }

        [DataMember(Name = "Incompatibilities")]
        public List<string> Incompatibilities { get; set; }

        [DataMember(Name = "ModDependencies")]
        public Dictionary<string, string> ModDependencies { get; set; }

        [DataMember(Name = "Url")]
        public string Url { get; set; }

        [DataMember(Name = "License")]
        public string License { get; set; }
    }

    [DataContract]
    public class LauncherV2ModsResponse
    {
        [DataMember(Name = "Response")]
        public Dictionary<string, SPTServerModInfo> Response { get; set; }
    }
}