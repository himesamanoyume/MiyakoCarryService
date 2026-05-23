
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

                // 检查老板生命值状态并且检测老板没有医疗物品，如果老板健康不行但是有医疗物品则无视
                if (false && false)
                {
                    // 根据老板的受伤状态再检查自己是否没有医疗物品
                    if (false && !BotOwner.Medecine.FirstAid.HaveSmth2Use)
                    {
                        // 尝试寻找周围的医疗物品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }

                    // 根据老板的受伤状态再检查自己是否没有手术包
                    if (false && !BotOwner.Medecine.SurgicalKit.HaveSmth2Use)
                    {
                        // 尝试寻找周围的手术包
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }

                    // 跑到老板旁边扔出医疗物品
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                }

                // 刷新自身受伤状态
                BotOwner.Medecine.GetDamaged();
                // 是否受到了非部位摧毁伤害
                if (BotOwner.Medecine.FirstAid.Damaged)
                {
                    // 是否没有医疗物品
                    if (!BotOwner.Medecine.FirstAid.HaveSmth2Use)
                    {
                        // 尝试寻找周围的医疗物品
                        // return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    else
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal:RunToCoverLogic1");
                        }
                    }
                }

                // 是否受到了部位摧毁伤害
                if (BotOwner.Medecine.SurgicalKit.Damaged)
                {
                    // 是否没有手术包
                    if (!BotOwner.Medecine.SurgicalKit.HaveSmth2Use)
                    {
                        // 尝试寻找周围的手术包
                        // return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    else
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal:RunToCoverLogic2");
                        }
                    }
                }

                // 老板健康无大碍，且医疗物品和掩体也都准备就绪后，才治疗自己
                if ((BotOwner.Medecine.FirstAid.Damaged && BotOwner.Medecine.FirstAid.HaveSmth2Use) || (BotOwner.Medecine.SurgicalKit.Damaged && BotOwner.Medecine.SurgicalKit.HaveSmth2Use))
                {
                    return new Action(typeof(HealLogic), "Mcs:Healing");
                }

                // 检查老板的吃喝状态是否低于阈值且老板身上没有吃喝
                if (false && false)
                {
                    // 是否是缺能量且自身没有能补充能量的食品
                    if (false && false)
                    {
                        // 尝试寻找周围的能补充能量的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    // 是否是缺水且自身没有能补充水的食品
                    else if (false && false)
                    {
                        // 尝试寻找周围的能补充水的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }

                    // 老板缺能量且自身有能补充能量的食品
                    if (false && false)
                    {
                        // 跑到老板旁边扔出能补充能量的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    // 老板缺水且自身有能补充水的食品
                    else if (false && false)
                    {
                        // 跑到老板旁边扔出能补充水的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                }

                // 老板吃喝无大碍
                if (false)
                {
                    // 自身是否是缺能量且自身没有能补充能量的食品
                    if (false && false)
                    {
                        // 尝试寻找周围的能补充能量的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                    // 自身是否是缺水且自身没有能补充水的食品
                    else if (false && false)
                    {
                        // 尝试寻找周围的能补充水的食品
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }

                    // 自身缺能量且自身有能补充能量的食品
                    if (false && false)
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal");
                        }
                    }
                    // 自身缺水且自身有能补充水的食品
                    else if (false && false)
                    {
                        // 进行治疗前先跑去掩体
                        if (!BotOwner.Memory.IsInCover)
                        {
                            return new Action(typeof(RunToCoverLogic), "Mcs:goforheal");
                        }
                    }

                    // 已位于掩体
                    if (BotOwner.Memory.IsInCover)
                    {
                        // 缺能量
                        if (false)
                        {
                            // 吃能补充能量的食品
                            return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                        }
                        // 缺水分
                        else if (false)
                        {
                            // 吃能补充水分的食品
                            return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                        }
                    }
                }

                // 检查当前包内战利品价值是否超过阈值，且老板身上是否还有空位
                if (false && false)
                {
                    // 跑到老板旁边扔出背包
                    return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                }

                // 当护航目前身上没穿着背包时，则认为当前正在上缴贡品
                if (false)
                {
                    // 检测老板与自身的距离，若超过一定距离则认为老板看不上剩下的物品了，于是重新拾取背包
                    if (false)
                    {
                        return new Action(typeof(SimplePatrolLogic), "Mcs:Basic");
                    }
                }

                if (McsBotPlayerData != null)
                {
                    // 检测周围是否有符合条件的战利品
                    if (McsBotPlayerData.McsAILeadPlayer.McsBotPlayerConfig.EnableLooting && McsBotPlayerData.LootingTarget != null)
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
                    if (Tools.BetterDestination(7f, newPos, out var targetPos))
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

                if (BotOwner.Position.McsSqrDistance(mcsLeadPlayerPos) >= _closeLeadDistance)
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
                    if (_lastPatrolTime < Time.time)
                    {
                        _lastPatrolTime = Time.time + 8f;
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