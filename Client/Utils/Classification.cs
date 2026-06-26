using System;
using System.Collections.Concurrent;
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
            nameof(EBrainName.BossZryachiy),
            nameof(EBrainName.Fl_Zraychiy),
            nameof(EBrainName.SctPredvst),
            nameof(EBrainName.PrizrakSt),
            nameof(EBrainName.Oni)
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

        public static Dictionary<ELabyrinthTrapType, HashSet<string>> LabyrinthTrapIds = new()
        {
            { ELabyrinthTrapType.Flame, new() {
                "flame_enter_zone_493925125",
                "flame_enter_zone_1349880479"
            }},
            { ELabyrinthTrapType.Valve, new() {
                "steam_enter_zone_115583983",
                "steam_enter_zone_3704346085"
            }},
            { ELabyrinthTrapType.Water, new() {
                "toxicZoneEnter1_2612876931",
                "toxicZoneEnter2_2612876931"
            }},
            { ELabyrinthTrapType.LiftingGrate, new() {
                "Luquid_enter_zone_3486811782",
                "Luquid_enter_zone_2773347541"
            }},
            { ELabyrinthTrapType.Gun, new() {
                "guntrap_trigger_1446364582",
                "guntrap_trigger_1628276106",
                "guntrap_trigger_2131128863",
                "guntrap_trigger_2505672369"
            }}
        };

        public static List<string> AllBrainNames = [.. Enum.GetNames(typeof(EBrainName))];

        public static Dictionary<string, string> ImportantSwitchIdsInfo = new()
        {
            { "autoId_000632_EXFIL", Locales.POWERSWITCH },
            { "autoId_00000_D2_LEVER", Locales.HERMETICSWITCH },
            { "00453", Locales.D2GATESWITCH },
            { "custom_DesignStuff_00034", Locales.ZB013POWERSWITCH },
            { "outside_buffer_trigger", Locales.LIGHTHOUSEGATEWAYOUTSIDESWITCH },
            { "inner_buffer_trigger", Locales.LIGHTHOUSEGATEWAYINNERSWITCH },
            { "switch_LighthouseGatewayZone_00000", Locales.PRYDOOR },
            { "00404", Locales.MAINELEVATORPOWERSWITCH },
            { "00409", Locales.MEDELEVATORPOWERSWITCH },
            { "autoId_00007_EXFIL", Locales.CARGOELEVATORPOWERSWITCH },
            { "autoId_00632_EXFIL", Locales.POWERSWITCH },
            { "00415", Locales.DRAINAGESWITCH },
            { "Use", Locales.CONTAINMENTBLOCKPOWERSWITCH },
            { "00418", Locales.BROADCASTSWITCH },
            { "autoId_00014_EXFIL", Locales.PARKINGGATESWITCH },
            { "switch_Labyrinth_DesignStaff_00000", Locales.ALARMSWITCH },
            { "switch_Labyrinth_DesignStaff_00001", Locales.ALARMSWITCH },
            { "switch_Labyrinth_DesignStaff_00002", Locales.ALARMSWITCH },
            { "switch_Labyrinth_DesignStaff_00003", Locales.FLAMETRAPSWITCH },
            { "switch_Labyrinth_DesignStaff_00004", Locales.ALARMSWITCH },
            { "switch_Labyrinth_DesignStaff_00005", Locales.POISONDRAINAGESWITCH },
            { "switch_Labyrinth_DesignStaff_00006", Locales.METALLOCK },
            { "switch_Labyrinth_DesignStuff_000033", Locales.METALLOCK },
            { "disable_traps_01", Locales.SHOTGUNTRAPSWITCH },
            { "disable_traps_04", Locales.POISONBRIDGESWITCH },
            { "disable_traps_07", Locales.ALARMSWITCH },
            { "set_valve_01", Locales.GASVALVE },
            { "Shopping_Mall_DesignStuff_00055", Locales.SAFEROOMPOWERSWITCH },
            { "Shopping_Mall_DesignStuff_00057", Locales.KIBASHOPALARMSWITCH },
            { "Shopping_Mall_DesignStuff_00058", Locales.BROADCASTSWITCH },
            { "Shopping_Mall_DesignStuff_00059", Locales.ALARMSWITCH },
            { "Shopping_Mall_DesignStuff_00060", Locales.SAFEROOMLOCKDOORSWITCH },
            { "Shopping_Mall_DesignStuff_00061", Locales.GENERATORSWITCH },
            { "Shopping_Mall_DesignStuff_00064", Locales.FLUSHURINAL }
        };
    }
}
