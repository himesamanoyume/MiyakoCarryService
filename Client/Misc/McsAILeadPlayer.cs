
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Models;
using UnityEngine;

namespace MiyakoCarryService.Client.Misc
{
    internal class McsAILeadPlayer : AIBossPlayer
    {
        public McsBotPlayerConfig McsBotPlayerConfig;
        public Player MyPlayer;
        public McsAILeadPlayer(Player player, McsBotPlayerConfig mcsBotPlayerConfig) : base(player)
        {
            MyPlayer = player;
            McsBotPlayerConfig = mcsBotPlayerConfig;
        }

        private static McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

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

                    // if (!enemyInfo.HaveSeen)
                    // {
                    //     continue;
                    // }

                    // if (!enemyInfo.ShallKnowEnemy())
                    // {
                    //     continue;
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
                // MiyakoCarryServicePlugin.Logger.LogWarn($"{botOwner.Profile.Info.Nickname} CalcGoalEnemy中即将调用AddEnemy");
                MyPlayer.BotsGroup.AddEnemy(closestEnemy.Person.AIData.BotOwner, EBotEnemyCause.byKill);
                MyPlayer.BotsGroup.ReportAboutEnemy(closestEnemy.Person.AIData.BotOwner, EEnemyPartVisibleType.Sence, botOwner);
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
                var distance = (Position - enemyInfo.CurrPosition).sqrMagnitude;
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