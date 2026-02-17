
using MiyakoCarryService.Server.Models.Enums;

namespace MiyakoCarryService.Server.Utils
{
    internal static class Tools
    {
        public static string GetBotTypeName(EBotType botType)
        {
            return botType switch
            {
                EBotType.bossBoar => "Kaban",
                EBotType.bossBully => "Reshala",
                EBotType.bossGluhar => "Glukhar",
                EBotType.bossKilla => "Killa",
                EBotType.bossKnight => "Knight",
                EBotType.followerBigPipe => "BigPipe",
                EBotType.followerBirdEye => "BirdEye",
                EBotType.bossKolontay => "Kolontay",
                EBotType.bossKojaniy => "Shturman",
                EBotType.bossSanitar => "Sanitar",
                EBotType.bossTagilla => "Tagilla",
                EBotType.bossPartisan => "Partisan",
                EBotType.bossZryachiy => "Zryachiy",
                EBotType.bossTagillaAgro => "Tagilla Agro",
                EBotType.bossKillaAgro => "Killa Agro",
                EBotType.infectedTagilla => "Infected Tagilla",
                EBotType.exUsec => "Rouge",
                EBotType.infectedPmc => "Infected Pmc",
                EBotType.infectedAssault => "Infected Assault",
                EBotType.pmcBot => "Raider",
                _ => "Common"
            };
        }
    }
}