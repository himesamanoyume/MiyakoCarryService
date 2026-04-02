using System;
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Enums;

namespace MiyakoCarryService.Client.Utils
{
    public sealed class Classification
    {
        public static HashSet<WildSpawnType> BossTypes = new()
        {
            WildSpawnType.bossBoar,
            WildSpawnType.bossBully,
            WildSpawnType.bossGluhar,
            WildSpawnType.bossKilla,
            WildSpawnType.bossKnight,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.bossKolontay,
            WildSpawnType.bossKojaniy,
            WildSpawnType.bossSanitar,
            WildSpawnType.bossTagilla,
            WildSpawnType.bossPartisan,
            WildSpawnType.bossZryachiy,
            WildSpawnType.ravangeZryachiyEvent,
            WildSpawnType.peacefullZryachiyEvent,
            WildSpawnType.gifter,
            WildSpawnType.arenaFighterEvent,
            WildSpawnType.sectantPriest,
            WildSpawnType.sectantPredvestnik,
            WildSpawnType.sectantPrizrak,
            WildSpawnType.sectantOni,
            WildSpawnType.shooterBTR,
            (WildSpawnType)199,
            (WildSpawnType)801,
            WildSpawnType.bossTagillaAgro,
            WildSpawnType.bossKillaAgro,
            WildSpawnType.infectedTagilla
        };

        public static HashSet<WildSpawnType> FollowerTypes = new()
        {
            WildSpawnType.followerGluharAssault,
            WildSpawnType.followerGluharSecurity,
            WildSpawnType.followerGluharScout,
            WildSpawnType.followerGluharSnipe,
            WildSpawnType.followerZryachiy,
            WildSpawnType.followerBoar,
            WildSpawnType.bossBoarSniper,
            WildSpawnType.followerBoarClose1,
            WildSpawnType.followerBoarClose2,
            WildSpawnType.sectantWarrior,
            WildSpawnType.followerBully,
            WildSpawnType.followerKojaniy,
            WildSpawnType.followerKolontayAssault,
            WildSpawnType.followerKolontaySecurity,
            WildSpawnType.followerSanitar
        };

        public static List<string> SAINNotAdjusted = new()
        {
            nameof(EBrainName.BossTest),
            nameof(EBrainName.Marksman),
            nameof(EBrainName.SectantWarrior),
            // nameof(EBrainName.SctPredvst),
            nameof(EBrainName.FlKlnAslt),
            nameof(EBrainName.KolonSec),
            nameof(EBrainName.Fl_Zraychiy),
        };

        public static HashSet<string> LabyrinthSolvePuzzleItems = new()
        {
            CommonId.BBQS43_GasTorch,
            CommonId.ValveHandwheel,
            CommonId.LabyrinthKey01,
            CommonId.LabyrinthKey02,
            CommonId.LabyrinthKey03,
            CommonId.LabyrinthKey04
        };

        public static HashSet<string> MoneyItems = new()
        {
            CommonId.Roubles,
            CommonId.Dollars,
            CommonId.Euros,
            CommonId.GPCoins
        };

        public static HashSet<WildSpawnType> FriendlyTypes = new()
        {
            WildSpawnType.shooterBTR,
            WildSpawnType.gifter,
            WildSpawnType.peacefullZryachiyEvent,
            WildSpawnType.bossZryachiy,
            WildSpawnType.followerZryachiy
        };

        public static HashSet<EBotEnemyCause> InitialBotEnemyCauses = new()
        {
            EBotEnemyCause.initial,
            EBotEnemyCause.AddNewMember,
            EBotEnemyCause.warn,
            EBotEnemyCause.addBotNoGroup
        };

        public static List<string> AllBrainNames = [.. Enum.GetNames(typeof(EBrainName))];
    }
}
