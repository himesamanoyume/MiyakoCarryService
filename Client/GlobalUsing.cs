global using SessionBackendClass = Class308;
global using CorpseTraderControllerClass = GClass3385;
global using TraderUtilsClass = GClass3130;
global using IOperationClass = GInterface438;
global using ContainerClass = GClass3248;
global using EFTVersionInfoClass = Class1123;

// 意图
global using BaseIntent = GClass26;
global using FireIntent = GClass27;
global using TimedFireIntent = GClass28;
global using FlankIntent = GClass29;
global using MoveIntent = GClass30;
global using CoverIntent = GClass31;

// 攻击与射击行为
global using AttackMovingFlankBaseLogic = GClass209;
global using AttackMovingBaseLogic = GClass205;
global using AttackMovingWithSuppressBaseLogic = GClass206;
global using DogFightBaseLogic = GClass203;
global using ShootFromCoverBaseLogic = GClass277;
global using ShootFromPlaceBaseLogic = GClass276;
global using ShootFromStationaryBaseLogic = GClass280;
global using ShootToSmokeBaseLogic = GClass185;

// 移动与路径行为
global using CrawlBaseLogic = GClass207;
global using GoToCoverPointBaseLogic = GClass212;
global using GoToCoverPointTacticalBaseLogic = GClass238;
global using GoToEnemyBaseLogic = GClass223;
global using GoToEnemyZigZagBaseLogic = GClass225;
global using GoToPointBaseLogic = GClass219;
global using GoToPointTacticalBaseLogic = GClass239;
global using MoveStealthyBaseLogic = GClass210;
global using RunAndThrowGrenadeFromPlaceBaseLogic = GClass286;
global using RunAwayArtilleryBaseLogic = GClass230;
global using RunAwayBTRBaseLogic = GClass231;
global using RunAwayGrenadeBaseLogic = GClass232;
global using RunToCoverBaseLogic = GClass228;
global using RunToCoverZigZagBaseLogic = GClass229;
global using RunToEnemyBaseLogic = GClass227;
global using RunToEnemyZigZagBaseLogic = GClass226;
global using RunToStationaryBaseLogic = GClass234;

// 投掷与特殊武器
global using PlantMineBaseLogic = GClass272;
global using DeactivateMineBaseLogic = GClass201;
global using SuppressGrenadeBaseLogic = GClass195;
global using ThrowGrenadeFromPlaceBaseLogic = GClass287;

// 治疗与物品交互
global using HealAnotherTargetBaseLogic = GClass196;
global using HealBaseLogic = GClass197;
global using HealStimulatorsBaseLogic = GClass283;
global using BotDropItemBaseLogic = GClass264;
global using BotTakeItemBaseLogic = GClass265;

// 姿态与状态
global using LayBaseLogic = GClass198;
global using PanicSittingBaseLogic = GClass260;
global using StandByBaseLogic = GClass282;
global using HoldPositionBaseLogic = GClass278;

// 巡逻与跟随
global using AlternativePatrolBaseLogic = GClass247;
global using FollowerPatrolBaseLogic = GClass248;
global using FollowMeRequestBaseLogic = GClass215;
global using FollowPlayerBaseLogic = GClass204;
global using SimplePatrolBaseLogic = GClass250;

// 和平/非战斗行为
global using AxeTargetBaseLogic = GClass246;
global using EatDrinkBaseLogic = GClass261;
global using FriendlyTiltBaseLogic = GClass262;
global using GestureBaseLogic = GClass263;
global using PeacefulBaseLogic = GClass266;
global using PeaceHardAimBaseLogic = GClass267;
global using PeaceLookBaseLogic = GClass268;
global using WatchSecondWeaponBaseLogic = GClass271;

// 事件与特殊
global using DeadBodyBaseLogic = GClass202;
global using DoorOpenBaseLogic = GClass259;
global using FlashedBaseLogic = GClass188;
global using GrenadeSuicideBaseLogic = GClass194;
global using LeaveMapBaseLogic = GClass243;
global using OneMeleeAttackBaseLogic = GClass242;
global using RepairMalfunctionBaseLogic = GClass273;
global using SuppressFireBaseLogic = GClass281;
global using SuppressStationaryBaseLogic = GClass284;
global using TeleportToCoverBaseLogic = GClass258;
global using TurnAwayLightBaseLogic = GClass288;
global using WarnPlayerBaseLogic = GClass289;

// 圣诞节事件
global using DoGiftChristmasEventBaseLogic = GClass192;
global using KhorovodChristmasEventBaseLogic = GClass191;

// 召唤
global using SummonBaseLogic = GClass193;

// 调试行为
global using DebugDropBaseLogic = GClass291;
global using DebugGestusBaseLogic = GClass263;        // 注意：与 GestureLogic 相同类
global using DebugGrenadeBaseLogic = GClass296;
global using DebugLayBaseLogic = GClass297;
global using DebugMeleeChangeBaseLogic = GClass295;
global using DebugMeleeBaseLogic = GClass299;
global using DebugMedsBaseLogic = GClass298;
global using DebugMoveBaseLogic = GClass252;
global using DebugRotateHeadBaseLogic = GClass300;
global using DebugRotateLayBaseLogic = GClass302;
global using DebugRotateBaseLogic = GClass301;
global using DebugRunBaseLogic = GClass255;
global using DebugRunToPointBaseLogic = GClass256;
global using DebugRunToCloseCoverBaseLogic = GClass254;
global using DebugShootBaseLogic = GClass304;
global using DebugStationaryInstantTakeBaseLogic = GClass306;
global using DebugStationaryBaseLogic = GClass305;
global using DebugTacticalShuttleBaseLogic = GClass240;
global using DebugtacticalMoveBaseLogic = GClass241;
global using DebugToggleLauncherBaseLogic = GClass308;
global using DebugWeaponChangeBaseLogic = GClass307;
global using DebugZigZagRunNodeBaseLogic = GClass257;
global using DebugShuttleBaseLogic = GClass253;

// 节点目标行为
global using GoToExfiltrationPointNodeBaseLogic = GClass244;
global using GoToLootPointNodeBaseLogic = GClass245;