using System.Collections.Generic;
using MiyakoCarryService.Server.Models.Enums;

namespace MiyakoCarryService.Server.Utils
{
    internal sealed class Classification
    {
        public static HashSet<EBotType> BossTypes = new()
        {
            EBotType.bossBoar,
            EBotType.bossBully,
            EBotType.bossGluhar,
            EBotType.bossKilla,
            EBotType.bossKnight,
            EBotType.followerBigPipe,
            EBotType.followerBirdEye,
            EBotType.bossKolontay,
            EBotType.bossKojaniy,
            EBotType.bossSanitar,
            EBotType.bossTagilla,
            EBotType.bossPartisan,
            EBotType.bossZryachiy,
            EBotType.bossTagillaAgro,
            EBotType.bossKillaAgro,
            EBotType.infectedTagilla
        };
    }
}
