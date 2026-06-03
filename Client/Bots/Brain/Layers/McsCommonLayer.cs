
using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsCommonLayer : McsBaseLayer<McsCommonLayer>
    {
        public McsCommonLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {
            InitActionMap();
        }

        public override void Start()
        {
            base.Start();
            _nextLootingCheckTime = Time.time + ENTER_COMMON_LOOTING_COLDDOWN;
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override Action GetNextAction()
        {
            try
            {
                if (McsBotPlayerData != null)
                {
                    if (McsBotPlayerData.ShouldGoToPoint)
                    {
                        return new Action(typeof(GoToPointLogic), "Mcs:GoToPointCommand");
                    }

                    if (McsBotPlayerData.ShouldHoldPosition)
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:HoldPositionCommand");
                    }
                }

                // 刷新自身受伤状态
                BotOwner.Medecine.GetDamaged();
                // 是否受到了非部位摧毁伤害
                if (BotOwner.Medecine.FirstAid.Damaged)
                {
                    // 是否没有医疗物品
                    if (BotOwner.Medecine.FirstAid.HaveSmth2Use)
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:RunToCoverForFirstAid");
                        }
                    }
                }

                // 是否受到了部位摧毁伤害
                if (BotOwner.Medecine.SurgicalKit.Damaged)
                {
                    // 是否没有手术包
                    if (BotOwner.Medecine.SurgicalKit.HaveSmth2Use)
                    {
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:RunToCoverForSurgical");
                        }
                    }
                }

                // 老板健康无大碍，且医疗物品和掩体也都准备就绪后，才治疗自己
                if ((BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use))
                {
                    return new Action(typeof(HealLogic), "Mcs:Healing");
                }

                CheckWeaponSwitch();

                if (McsBotPlayerData != null)
                {
                    // 检测周围是否有符合条件的战利品
                    if (McsBotPlayerData.McsAILeadPlayer.McsBotPlayerConfig.EnableLooting && McsBotPlayerData.LootingTarget != null && _nextLootingCheckTime < Time.time)
                    {
                        // 尝试去拿战利品
                        return new Action(typeof(GoToLootTargetLogic), "Mcs:GoToLootTarget");
                    }
                }

                var mcsLeadPlayerPos = GetMcsLeadPlayerPos();
                if (mcsLeadPlayerPos == null)
                {
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:leadPosNull");
                }

                Vector3? validPosition = null;
                var xOffset = GClass856.Random(3f, 4f) * GClass856.RandomSing();
                var zOffset = GClass856.Random(3f, 4f) * GClass856.RandomSing();
                var newPos = mcsLeadPlayerPos + new Vector3(xOffset, 0f, zOffset);

                for (int attempt = 0; attempt < 30; attempt++)
                {
                    if (Tools.BetterDestination(3f, newPos, out var targetPos))
                    {
                        if (Mathf.Abs(targetPos.y - mcsLeadPlayerPos.y) <= 2f)
                        {
                            validPosition = targetPos;
                            break;
                        }
                    }
                }

                if (validPosition == null && NavMesh.SamplePosition(newPos, out var navMeshHit, 7f, -1))
                {
                    validPosition = navMeshHit.position;
                }

                if (BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= TOO_FAR_FROM_LEAD_DISTANCE * TOO_FAR_FROM_LEAD_DISTANCE)
                {
                    if (validPosition.HasValue)
                    {
                        BotOwner.GoToSomePointData.SetPoint(validPosition.Value);
                        return new Action(typeof(GoToPointLogic), "Mcs:GoToPointLogic");
                    }

                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:CannotFindPath1");
                }
                else
                {
                    if (_nextPatrolTime < Time.time)
                    {
                        _nextPatrolTime = Time.time + 8f;
                        if (validPosition.HasValue)
                        {
                            BotOwner.GoToSomePointData.SetPoint(validPosition.Value);
                            return new Action(typeof(GoToPointLogic), "Mcs:Partoling");
                        }

                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:CannotFindPath2");
                    }
                    else
                    {
                        return new Action(typeof(HoldPositionLogic), "Mcs:HoldPosition");
                    }
                }
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(SimplePatrolLogic), "Mcs:Basic:Exception");
            }
        }

        public override bool IsActive()
        {
            if (IsMcsBotPlayer)
            {
#if DEBUG
                if (!MiyakoCarryServicePlugin.EnableMcsLayer.Value)
                {
                    return false;
                }
#endif
                if (BotOwner.Memory.IsUnderFire)
                {
                    return false;
                }
                if (BotOwner.Memory.HaveEnemy && MiyakoCarryServicePlugin.SAINInstalled)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}