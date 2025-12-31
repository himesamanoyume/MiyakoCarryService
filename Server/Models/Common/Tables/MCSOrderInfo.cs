
using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Common;
using MiyakoCarryService.Server.Models.Enums;

namespace MiyakoCarryService.Server.Models.Common.Tables
{
    public record MCSOrderInfo
    {
        public MongoId SessionID { get; set; }
        public HashSet<MongoId> CarryServicePlayerIds { get; set; }
        public int CarryServiceLevel { get; set; }
        public long EndTime { get; set; }
        public EOrderInfoStatus Status { get; set; }
    }
}