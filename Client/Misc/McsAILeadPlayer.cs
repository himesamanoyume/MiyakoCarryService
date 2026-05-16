
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Misc
{
    public class McsAILeadPlayer : AIBossPlayer
    {
        public McsBotPlayerConfig McsBotPlayerConfig;
        public Player MyPlayer;
        public McsAILeadPlayer(Player player, McsBotPlayerConfig mcsBotPlayerConfig) : base(player)
        {
            MyPlayer = player;
            McsBotPlayerConfig = mcsBotPlayerConfig;
        }

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        public void CalcGoalEnemy()
        {
            var list = new List<EnemyInfo>();
            var mcsBotPlayerBotOwners = McsMgr.GetAllMcsSquadMembersByMcsLeadId(Player().ProfileId);
            foreach (var botOwner in mcsBotPlayerBotOwners)
            {
                foreach (var enemyInfo in botOwner.EnemiesController.EnemyInfos.Values)
                {
                    if (!enemyInfo.Person.HealthController.IsAlive)
                    {
                        continue;
                    }

                    // if (enemyInfo.IsVisible)
                    // {
                    //     MiyakoCarryServicePlugin.Logger.LogError("因有可见的敌人，将该敌人设置全体护航的目标敌人");
                    //     foreach (var _botOwner in mcsBotPlayerBotOwners)
                    //     {
                    //         MyPlayer.BotsGroup.AddEnemy(enemyInfo.Person.AIData.BotOwner, EBotEnemyCause.byKill);
                    //         MyPlayer.BotsGroup.ReportAboutEnemy(enemyInfo.Person.AIData.BotOwner, EEnemyPartVisibleType.Visible, _botOwner);
                    //         enemyInfo.IsVisible = true;
                    //         _botOwner.Memory.GoalEnemy = enemyInfo;
                    //         enemyInfo.PriorityIndex = 0;
                    //     }
                    //     return;
                    // }

                    list.Add(enemyInfo);
                }
            }

            if (list.Count == 0)
            {
                return;
            }

            var closestEnemy = GetClosestEnemy(list);
            if (closestEnemy == null)
            {
                return;
            }

            foreach (var botOwner in mcsBotPlayerBotOwners)
            {
                MyPlayer.BotsGroup.AddEnemy(closestEnemy.Person.AIData.BotOwner, EBotEnemyCause.byKill);
                MyPlayer.BotsGroup.ReportAboutEnemy(closestEnemy.Person.AIData.BotOwner, EEnemyPartVisibleType.Visible, botOwner);
                closestEnemy.IsVisible = true;
                botOwner.Memory.GoalEnemy = closestEnemy;
                closestEnemy.PriorityIndex = 0;
            }
        }

        public EnemyInfo GetClosestEnemy(List<EnemyInfo> enemiesInfos)
        {
            if (enemiesInfos.Count == 0)
            {
                return null;
            }

            EnemyInfo closestEnemy = null;

            var minDistance = Mathf.Infinity;
            foreach (var enemyInfo in enemiesInfos)
            {
                var distance = Position.McsSqrDistance(enemyInfo.CurrPosition);
                if (distance < minDistance)
                {
                    closestEnemy = enemyInfo;
                    minDistance = distance;
                }
            }

            return closestEnemy;
        }
    }
}