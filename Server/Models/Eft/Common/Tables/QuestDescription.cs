
namespace MiyakoCarryService.Server.Models.Eft.Common.Tables
{
    public record QuestDescription
    {
        public int Players { get; set; }
        public SpawnType SpawnType { get; set; }
        public int CarryServiceLevel { get; set; }
        public int Duration { get; set; }
        public double Fines { get; set; }
    }
}