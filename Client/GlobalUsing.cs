global using SessionBackendClass = EFT.EftClientBackendSession;
global using CorpseTraderControllerClass = EFT.InventoryLogic.CorpseItemController;
global using TraderUtilsClass = EFT.InventoryLogic.CurrencyUtil;
global using IOperationClass = EFT.InventoryLogic.Operations.IInventoryOperation;
global using ContainerClass = EFT.InventoryLogic.ContainerCollection;
global using EFTVersionInfoClass = BuildInfo;
global using SearchControllerClass = EFT.SearchController;
global using ContainerDataClass = EFT.ItemInfo;
global using InventoryActionClass = EFT.InteractionContextHelper.CG_GetAvailableInteractionState1;
global using ItemSubtractClass1 = EFT.Player.PlayerInventoryController.CG_SubtractFromDiscardLimits;
global using ItemSubtractClass2 = EFT.Player.PlayerInventoryController.CG_AddDiscardLimits;
global using UnknownItemManipulationClass = EFT.InventoryLogic.UnknownItemError;
global using ItemLockedClass = EFT.InventoryLogic.ItemManipulator.ItemManuallyLockedError;
global using CannotMoveItemDuringRaidClass = EFT.InventoryLogic.ItemManipulator.CantRemoveFromEquipmentSlotDuringRaid;
global using HeavyBleedEffect = EFT.HealthSystem.IHeavyBleeding;
global using LightBleedEffect = EFT.HealthSystem.ILightBleeding;
global using FractureEffect = EFT.HealthSystem.IFracture;

// 意图
global using BaseIntent = CoreActionResultParams;
global using FireIntent = AimingResultParams;
global using TimedFireIntent = ShootHoldResultParams;
global using FlankIntent = CoreActionResultParamsFlankMove;
global using MoveIntent = CoreActionResultGoToPoint;
global using CoverIntent = MoveToCoverActionResultData;

// 攻击与射击行为
global using AttackMovingFlankBaseLogic = AttackMovingFlank;
global using AttackMovingBaseLogic = AttackMoving;
global using AttackMovingWithSuppressBaseLogic = AttackMovingWithSuppress;
global using DogFightBaseLogic = DogFightNode;
global using ShootFromCoverBaseLogic = ShootFromCover;
global using ShootFromPlaceBaseLogic = ShootFromPlace;
global using ShootFromStationaryBaseLogic = ShootFromStationary;
global using ShootToSmokeBaseLogic = AimingToSmoke;

// 移动与路径行为
global using CrawlBaseLogic = CrawlNode;
global using GoToCoverPointBaseLogic = GoToCoverPoint;
global using GoToCoverPointTacticalBaseLogic = GoToCoverTactical;
global using GoToEnemyBaseLogic = GoToEnemy;
global using GoToEnemyZigZagBaseLogic = GoToEnemyZigZag;
global using GoToPointBaseLogic = GoToSomePoint;
global using GoToPointTacticalBaseLogic = GoToPointTacticalNode;
global using MoveStealthyBaseLogic = MoveStealthy;
global using RunAndThrowGrenadeFromPlaceBaseLogic = GoToGrenadeRequestNode;
global using RunAwayArtilleryBaseLogic = RunAwayArtillery;
global using RunAwayBTRBaseLogic = RunAwayBTR;
global using RunAwayGrenadeBaseLogic = RunAwayGrenade;
global using RunToCoverBaseLogic = RunToCover;
global using RunToCoverZigZagBaseLogic = RunToCoverZigZag;
global using RunToEnemyBaseLogic = RunToEnemy;
global using RunToEnemyZigZagBaseLogic = RunToEnemyZigZag;
global using RunToStationaryBaseLogic = RunToStationary;

// 投掷与特殊武器
global using PlantMineBaseLogic = PlantMineNode;
global using DeactivateMineBaseLogic = DeactivateMineNode;
global using SuppressGrenadeBaseLogic = GrenadeSuppressNode;
global using ThrowGrenadeFromPlaceBaseLogic = ThrowGrenadeFromPlaceNode;

// 治疗与物品交互
global using HealAnotherTargetBaseLogic = HealAnotherNode;
global using HealBaseLogic = HealNode;
global using HealStimulatorsBaseLogic = StimulatorsNode;
global using BotDropItemBaseLogic = PatrolDropItemsNode;
global using BotTakeItemBaseLogic = PatrolTakeItemsNode;

// 姿态与状态
global using LayBaseLogic = LayNode;
global using PanicSittingBaseLogic = PanicSitNode;
global using StandByBaseLogic = StandByNode;
global using HoldPositionBaseLogic = HoldPosition;

// 巡逻与跟随
global using AlternativePatrolBaseLogic = PatrollingAlternative;
global using FollowerPatrolBaseLogic = PatrollingFollower;
global using FollowMeRequestBaseLogic = FollowMeRequest;
global using FollowPlayerBaseLogic = PatrollingFollowerPlayer;
global using SimplePatrolBaseLogic = PatrolSimpleNode;

// 和平/非战斗行为
global using AxeTargetBaseLogic = PatrolAxeTarget;
global using EatDrinkBaseLogic = EatDrinkNode;
global using FriendlyTiltBaseLogic = FriendlyTiltNode;
global using GestureBaseLogic = GestureNode;
global using PeacefulBaseLogic = PeacefulNode;
global using PeaceHardAimBaseLogic = PeaceHardAimNode;
global using PeaceLookBaseLogic = PeaceLookNode;
global using WatchSecondWeaponBaseLogic = WatchSecondWeaponNode;

// 事件与特殊
global using DeadBodyBaseLogic = DeadBodiesWorkNode;
global using DoorOpenBaseLogic = OpenDoorRequestDecision;
global using FlashedBaseLogic = FlashedNode;
global using GrenadeSuicideBaseLogic = GrenadeSuicideNode;
global using LeaveMapBaseLogic = GoLeaveNode;
global using MeleeAttackBaseLogic = OneMeleeAttackNode;
global using RepairMalfunctionBaseLogic = RepairMalfunctionNode;
global using SuppressFireBaseLogic = ShootSuppressNode;
global using SuppressStationaryBaseLogic = SuppressStationaryNode;
global using TeleportToCoverBaseLogic = TeleportNode;
global using TurnAwayLightBaseLogic = TurnAwayNode;
global using WarnPlayerBaseLogic = WarnPlayerAttentionNode;

// 圣诞节事件
global using DoGiftChristmasEventBaseLogic = GiftNode;
global using KhorovodChristmasEventBaseLogic = BotKhorovodNode;

// 召唤
global using SummonBaseLogic = SummonNode;

// 节点目标行为
global using GoToExfiltrationPointNodeBaseLogic = GoToExfiltrationPointNode;
global using GoToLootPointNodeBaseLogic = GoToLootPointNode;