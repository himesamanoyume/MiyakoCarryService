
using System.Runtime.Serialization;

namespace MiyakoCarryService.Client.Models
{
    [DataContract]
    public class SPTServerModInfo
    {
        [DataMember(Name = "InServer")]
        public bool InServer { get; set; }

        [DataMember(Name = "InProfile")]
        public bool InProfile { get; set; }

        [DataMember(Name = "Author")]
        public string Author { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Version")]
        public string Version { get; set; }

        [DataMember(Name = "ModGuid")]
        public string ModGuid { get; set; }

        [DataMember(Name = "License")]
        public string License { get; set; }

        [DataMember(Name = "SPTVersion")]
        public string SPTVersion { get; set; }
    }
}