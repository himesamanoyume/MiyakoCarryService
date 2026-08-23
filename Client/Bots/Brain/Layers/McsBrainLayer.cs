using System;
using EFT;
using MiyakoCarryService.Client.Bots.Brain.Logics;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Bots.Brain.Layers
{
    public class McsBrainLayer : McsBaseLayer
    {
        public McsBrainLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
        {

        }

        public float _contactTime = 0f;
        public float _nextRecalcGoalTime = 0f;
        public const float FightHoldTime = 3f;
        public bool _deferToSain = false;
        public float _goToStationaryStuckTime = -999f;
        public float _lastSqrToOperator = float.MaxValue;

        public override Action GetNextAction()
        {
            try
            {
                #region McsAvoidDangerLayer
                if (BotOwner.FlashGrenade.IsFlashed)
                {
                    return new Action(typeof(FlashedLogic), "Mcs:Flashed");
                }

                if (BotOwner.BotTurnAwayLight.IsActive)
                {
                    return new Action(typeof(HoldPositionLogic), "Mcs:TurnAwayLight");
                }

                if (BotOwner.ArtilleryDangerPlace.ShallRunAway())
                {
                    return new Action(typeof(RunAwayArtilleryLogic), "Mcs:RunAwayArtillery");
                }

                if (BotOwner.BewareGrenade.ShallRunAway())
                {
                    return new Action(typeof(RunAwayGrenadeLogic), "Mcs:RunAwayGrenade");
                }

                if (BotOwner.BewareBTR.ShallRunAway())
                {
                    return new Action(typeof(RunAwayBTRLogic), "Mcs:RunAwayBTR");
                }

                if (!BotOwner.Memory.HaveEnemy && BotOwner.SmokeGrenade.IsInSmoke)
                {
                    return new Action(typeof(GoToCoverPointLogic), "Mcs:PeaceSmoke");
                }

                if (BotOwner.BewarePlantedMine.CanDeactivate())
                {
                    return new Action(typeof(DeactivateMineLogic), "Mcs:DeactivateMine");
                }

                // return new Action(typeof(HoldPositionLogic), "Mcs:NoDanger");
                #endregion

                #region McsProxyLayer

                #endregion

                #region McsEscortLayer

                #endregion

                #region McsFightLayer

                #endregion

                #region McsExfiltrationLayer

                #endregion

                #region McsClearAreaLayer

                #endregion

                #region McsCommonLayer

                #endregion

                return new Action(typeof(HoldPositionLogic), "Mcs:Default");
            }
            catch (Exception e)
            {
                MiyakoCarryServicePlugin.Logger.LogError(e);
                return new Action(typeof(HoldPositionLogic), "Mcs:Exception");
            }
        }

        public override bool IsActive()
        {
            if (!MiyakoCarryServicePlugin.UseUnifiedBrainLayer.Value)
            {
                return false;
            }
            
            if (!IsMcsBotPlayer)
            {
                return false;
            }

#if DEBUG
            if (!MiyakoCarryServicePlugin.EnableMcsLayer.Value)
            {
                return false;
            }
#endif

            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();

            if (BotOwner.Memory.HaveEnemy)
            {
                var goalEnemy = BotOwner.Memory.GoalEnemy;
                var enemyExist = goalEnemy != null && goalEnemy.Person != null;
                // 使护航下的Zyriachy无视目标处于灯塔限定区域时才可视为敌人的限制
                if (BotOwner.Profile.Info.Settings.Role is WildSpawnType.bossZryachiy or WildSpawnType.followerZryachiy)
                {
                    if (enemyExist)
                    {
                        if (BotOwner.Boss.BossLogic is BossZryachiy bossZryachiy)
                        {
                            bossZryachiy.AddEnemy(goalEnemy.Person, EBotEnemyCause.zryachiyLogic);
                        }
                    }
                }

                var mcsLeadPlayerPos = BotOwner.GetMcsLeadPlayerPos(mcsBotPlayerData);
                if (enemyExist && MiyakoCarryServicePlugin.SAINInstalled)
                {
                    var sqrDist = mcsLeadPlayerPos.McsSqrDistance(goalEnemy.Person.Position);
                    if (_deferToSain)
                    {
                        if (sqrDist > SAINUtils.ExitSainSqr)
                        {
                            _deferToSain = false;
                        }
                    }
                    else
                    {
                        if (sqrDist < SAINUtils.EnterSainSqr)
                        {
                            _deferToSain = true;
                        }
                    }

                    if (_deferToSain)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
